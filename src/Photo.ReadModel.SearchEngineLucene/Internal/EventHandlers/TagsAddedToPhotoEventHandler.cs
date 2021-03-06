﻿namespace EagleEye.Photo.ReadModel.SearchEngineLucene.Internal.EventHandlers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using CQRSlite.Events;
    using Dawn;
    using EagleEye.Photo.Domain.Events;
    using EagleEye.Photo.ReadModel.SearchEngineLucene.Internal.LuceneNet;
    using EagleEye.Photo.ReadModel.SearchEngineLucene.Internal.Model;
    using JetBrains.Annotations;

    [UsedImplicitly]
    internal class TagsAddedToPhotoEventHandler : ICancellableEventHandler<TagsAddedToPhoto>
    {
        [NotNull] private readonly IPhotoIndex photoIndex;

        public TagsAddedToPhotoEventHandler([NotNull] IPhotoIndex photoIndex)
        {
            Guard.Argument(photoIndex, nameof(photoIndex)).NotNull();
            this.photoIndex = photoIndex;
        }

        public async Task Handle(TagsAddedToPhoto message, CancellationToken token = default)
        {
            Guard.Argument(message, nameof(message)).NotNull();
            Guard.Argument(message.Tags, nameof(message.Tags)).NotNull();

            if (!(photoIndex.Search(message.Id) is Photo storedItem))
                return;

            storedItem.Version = message.Version;
            storedItem.Tags ??= new List<string>();

            var newEntries = message.Tags.Distinct()
                .Where(item => !storedItem.Tags.Contains(item))
                .ToArray();

            if (newEntries.Length == 0)
                return;

            storedItem.Tags.AddRange(newEntries);
            await photoIndex.ReIndexMediaFileAsync(storedItem).ConfigureAwait(false);
        }
    }
}
