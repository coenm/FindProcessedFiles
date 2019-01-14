﻿namespace EagleEye.Photo.Domain.Test
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using CQRSlite.Domain;
    using CQRSlite.Events;
    using CQRSlite.Routing;

    using EagleEye.Photo.Domain.CommandHandlers;
    using EagleEye.Photo.Domain.Commands;
    using EagleEye.Photo.Domain.Events;
    using EagleEye.Photo.Domain.EventStore;

    using FluentAssertions;

    using Xunit;

    public class DomainIntegrationTests
    {
        [Fact]
        public async Task Handle_CreateMediaItemCommand_ShouldPublishEventTest()
        {
            // arrange
            var publisher = new Router();
            var repository = new Repository(new InMemoryEventStore(publisher));
            var session = new Session(repository);
            var handler = new MediaItemCommandHandlers(session);
            var events = new List<IEvent>();
            publisher.RegisterHandler<PhotoCreated>((evt, ct) =>
                                                        {
                                                            events.Add(evt);
                                                            return Task.CompletedTask;
                                                        });
            publisher.RegisterHandler<TagsAddedToPhoto>((evt, ct) =>
                                                        {
                                                            events.Add(evt);
                                                            return Task.CompletedTask;
                                                        });

            // act
            var hash = new byte[32];
            var command = new CreatePhotoCommand("aap", hash, "image/jpeg", new[] { "zoo", "holiday" }, null);
            var guid = command.Id;
            await handler.Handle(command, default(CancellationToken)).ConfigureAwait(false);

            var addTagsCommand = new AddTagsToPhotoCommand(guid, 1, "summer", "holiday");
            await handler.Handle(addTagsCommand, default(CancellationToken)).ConfigureAwait(false);

            addTagsCommand = new AddTagsToPhotoCommand(guid, 2, "summer", "soccer");
            await handler.Handle(addTagsCommand, default(CancellationToken)).ConfigureAwait(false);

            var removeTagsCommand = new RemoveTagsFromPhotoCommand(guid, 3, "summer");
            await handler.Handle(removeTagsCommand, default(CancellationToken)).ConfigureAwait(false);

            // assert
            events.Should().HaveCount(3);
        }
    }
}