﻿namespace EagleEye.Photo.ReadModel.SearchEngineLucene
{
    using System;

    using EagleEye.Photo.ReadModel.SearchEngineLucene.Interface;
    using EagleEye.Photo.ReadModel.SearchEngineLucene.Internal;
    using EagleEye.Photo.ReadModel.SearchEngineLucene.Internal.EventHandlers;
    using EagleEye.Photo.ReadModel.SearchEngineLucene.Internal.LuceneDirectoryFactories;
    using EagleEye.Photo.ReadModel.SearchEngineLucene.Internal.LuceneNet;
    using Helpers.Guards;
    using JetBrains.Annotations;
    using SimpleInjector;

    public static class Bootstrapper
    {
        /// <summary> Bootstrap this module.</summary>
        /// <param name="container">The IOC container. Cannot be <c>null</c>.</param>
        /// <param name="useInMemoryIndex"><c>true</c> When the InMemory configuration should be used for Lucene, <c>false</c> otherwise.</param>
        /// <param name="baseDirectory">Base directory for the Lucene index files. Only required and used when <paramref name="useInMemoryIndex"/> is <c>false</c>. Should be a valid directory.</param>
        /// <exception cref="ArgumentNullException">Thrown when one of the required arguments is <c>null</c>.</exception>
        public static void BootstrapSearchEngineLuceneReadModel(
            [NotNull] Container container,
            bool useInMemoryIndex = true,
            [CanBeNull] string baseDirectory = null)
        {
            Guard.NotNull(container, nameof(container));
            if (useInMemoryIndex == false)
                Guard.NotNull(baseDirectory, nameof(baseDirectory));

            container.RegisterSingleton<PhotoIndex>();

            container.Register<IReadModel, LucenePhotoReadModel>();

            container.Register<PhotoCreatedEventHandler>();

            if (useInMemoryIndex)
                container.RegisterSingleton<ILuceneDirectoryFactory, RamLuceneDirectoryFactory>();
            else
                container.RegisterSingleton<ILuceneDirectoryFactory>(() => new FileSystemLuceneDirectoryFactory(baseDirectory));
        }

        public static Type[] GetEventHandlerTypes()
        {
            return new[]
            {
                typeof(PhotoCreatedEventHandler),
            };
        }
    }
}