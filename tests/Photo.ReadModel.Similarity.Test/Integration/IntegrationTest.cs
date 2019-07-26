﻿namespace Photo.ReadModel.Similarity.Test.Integration
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using CQRSlite.Events;
    using CQRSlite.Routing;
    using EagleEye.Core.Interfaces.Core;
    using EagleEye.Core.Interfaces.Module;
    using EagleEye.Photo.Domain.Events;
    using EagleEye.Photo.ReadModel.Similarity.Interface;
    using EagleEye.Photo.ReadModel.Similarity.Interface.Model;
    using EagleEye.Photo.ReadModel.Similarity.Internal.EntityFramework;
    using EagleEye.TestHelper;
    using FakeItEasy;
    using FakeItEasy.Sdk;
    using FluentAssertions;
    using SimpleInjector;
    using Xunit;
    using Xunit.Abstractions;

    public class IntegrationTest
    {
        private readonly ITestOutputHelper writer;
        private const string ExistingImageFilename = "1.jpg";
        private readonly IDateTimeService dateTimeService;
        private readonly Container container;
        private readonly IFileService fileService;
        private readonly DateTime dtNow;

        public IntegrationTest(Xunit.Abstractions.ITestOutputHelper writer)
        {
            this.writer = writer;
            container = new Container();
            dateTimeService = A.Fake<IDateTimeService>();
            dtNow = new DateTime(2000, 1, 2, 3, 4, 5);
            A.CallTo(() => dateTimeService.Now).Returns(dtNow);
            RegisterExternalDependencies(container);
            fileService = A.Fake<IFileService>();

            EagleEye.Photo.ReadModel.Similarity.Bootstrapper.Bootstrap(
               container,
               "InMemory a",
               "InMemory b");

            RegisterTestSynchronizationDbContextSaveEventPublisher(container);
            RegisterCqrsLiteStuff(container);

            container.Verify();

            A.CallTo(() => fileService.OpenRead(ExistingImageFilename))
             .ReturnsLazily(call => EagleEye.TestHelper.TestImages.ReadRelativeImageFile(ExistingImageFilename));

            TestImages.ReadRelativeImageFile(ExistingImageFilename).Should().NotBeNull("This testsuite relies on this.");
        }

        [Fact]
        public async Task EnablePlugin_And_ProvideHashesForImage()
        {
            var dbSaveHappenedService = container.GetInstance<ISimilarityDbContextSavedEventPublisher>();

            var initializers = container.GetAllInstances<IEagleEyeInitialize>().ToArray();
            initializers.Should().HaveCount(1);
            var initializer = initializers.Single();
            await initializer.InitializeAsync();

            var processes = container.GetAllInstances<IEagleEyeProcess>().ToArray();
            processes.Should().HaveCount(1);
            var process = processes.Single();
            process.Start();

            var similarityReadModel = container.GetInstance<ISimilarityReadModel>();
            var hashAlgorihms = await similarityReadModel.GetHashAlgorithmsAsync();
            hashAlgorihms.Should().BeEmpty();

            hashAlgorihms = await similarityReadModel.GetHashAlgorithmsAsync();
            hashAlgorihms.Should().BeEmpty();

            var publisher = container.GetInstance<IEventPublisher>();
            var photoGuid1 = Guid.NewGuid();
            var evt = new PhotoHashAdded(photoGuid1, "AverageHash", 13213);
            await publisher.Publish(evt, CancellationToken.None);

            hashAlgorihms = await similarityReadModel.GetHashAlgorithmsAsync();
            hashAlgorihms.Should().BeEquivalentTo("AverageHash");

            var photoGuid2 = Guid.NewGuid();
            evt = new PhotoHashAdded(photoGuid2, "AverageHash", 13212);
            await publisher.Publish(evt, CancellationToken.None);

            var similarPhotos = await similarityReadModel.CountSimilaritiesAsync(photoGuid1, "AverageHash", 50);
            if (similarPhotos == 0)
            {
                writer.WriteLine("count was 0 (1)");
                var dbSavedHappened = new AutoResetEvent(false);
                dbSaveHappenedService.DbSaveHappened += (_, __) => dbSavedHappened.Set();
                similarPhotos = await similarityReadModel.CountSimilaritiesAsync(photoGuid1, "AverageHash", 50);
                if (similarPhotos == 0)
                {
                    writer.WriteLine("count was 0 (2)");
                    dbSavedHappened.WaitOne(TimeSpan.FromSeconds(10));
                    similarPhotos = await similarityReadModel.CountSimilaritiesAsync(photoGuid1, "AverageHash", 50);
                }
            }

            similarPhotos.Should().Be(1);

            hashAlgorihms = await similarityReadModel.GetHashAlgorithmsAsync();
            hashAlgorihms.Should().BeEquivalentTo("AverageHash");

            similarPhotos = await similarityReadModel.CountSimilaritiesAsync(photoGuid1, "AverageHash", 100);
            similarPhotos.Should().Be(0);

            var matches = await similarityReadModel.GetSimilaritiesAsync(photoGuid1, "AverageHash", 95);
            matches.Should().BeEquivalentTo(new SimilarityResultSet(photoGuid1, dtNow, new SimilarityResult(photoGuid2, 98.4375)));

            matches = await similarityReadModel.GetSimilaritiesAsync(photoGuid1, "AverageHash", 100);
            matches.Should().BeEquivalentTo(new SimilarityResultSet(photoGuid1, dtNow));

            process.Stop();
            container.Dispose();
        }

        private static void RegisterCqrsLiteStuff(Container container)
        {
            container.Register<Router>(Lifestyle.Singleton);
            container.Register<IEventPublisher>(container.GetInstance<Router>, Lifestyle.Singleton);
            container.Register<IHandlerRegistrar>(container.GetInstance<Router>, Lifestyle.Singleton);

            // make sure that the CqrsLite lib has knowledge how to create the event handlers.
            // in the feature, this should be different.
            var registrar = new RouteRegistrar(container);
            registrar.RegisterHandlers(EagleEye.Photo.ReadModel.Similarity.Bootstrapper.GetEventHandlerTypes());
        }

        private static void RegisterTestSynchronizationDbContextSaveEventPublisher(Container container)
        {
            container.Register<ISimilarityDbContextSavedEventPublisher, SimilarityDbContextSavedEventPublisher>(Lifestyle.Singleton);
            container.RegisterDecorator<ISimilarityDbContextFactory, EventOnSaveDbContextDecoratorFactory>(Lifestyle.Singleton);
        }

        private void RegisterExternalDependencies(Container container)
        {
            foreach (var @type in EagleEye.Photo.ReadModel.Similarity.Bootstrapper.ExternalRequiredInterfaces())
            {
                if (type == typeof(IDateTimeService))
                {
                    container.RegisterInstance(dateTimeService);
                    continue;
                }

                container.Register(@type, () => Create.Dummy(@type));
            }
        }
    }
}