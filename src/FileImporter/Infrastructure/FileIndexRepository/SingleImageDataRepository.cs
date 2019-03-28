﻿namespace EagleEye.FileImporter.Infrastructure.FileIndexRepository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using EagleEye.FileImporter.Indexing;
    using Helpers.Guards; using Dawn;

    /// <summary>
    /// Stores data in file.
    /// </summary>
    public class SingleImageDataRepository : IImageDataRepository
    {
        private readonly IPersistentSerializer<List<ImageData>> storage;
        private readonly List<ImageData> data;
        private readonly object syncLock = new object();

        public SingleImageDataRepository(IPersistentSerializer<List<ImageData>> storage)
        {
            Dawn.Guard.Argument(storage, nameof(storage)).NotNull();
            this.storage = storage;
            data = this.storage.Load();
        }

        public ImageData Get(string identifier)
        {
            Dawn.Guard.Argument(identifier, nameof(identifier)).NotNull().NotEmpty();
            return data.FirstOrDefault(i => i.Identifier.Equals(identifier, StringComparison.InvariantCulture));
        }

        public IEnumerable<ImageData> Find(Predicate<ImageData> predicate, int take = 0, int skip = 0)
        {
            Dawn.Guard.Argument(predicate, nameof(predicate)).NotNull();

            var result = data.Where(index => predicate(index));

            if (skip > 0)
                result = result.Skip(skip);

            if (take > 0)
                result = result.Take(take);

            return result;
        }

        public IEnumerable<ImageData> FindSimilar(
            ImageData src,
            double minAvgHash = 95,
            double minDiffHash = 95,
            double minPerHash = 95,
            int take = 0,
            int skip = 0)
        {
            Dawn.Guard.Argument(src, nameof(src)).NotNull();

            var result = data.Where(index =>
                {
                    if (index.Identifier.Equals(src.Identifier, StringComparison.InvariantCulture))
                        return false;

                    if (index.Hashes.FileHash.SequenceEqual(src.Hashes.FileHash))
                        return true;

                    if (index.Hashes.ImageHash.SequenceEqual(src.Hashes.ImageHash))
                        return true;

                    double value;

                    if (minAvgHash >= 0 && minAvgHash <= 100)
                    {
                        value = CoenM.ImageHash.CompareHash.Similarity(index.Hashes.AverageHash, src.Hashes.AverageHash);
                        if (value >= minAvgHash)
                            return true;
                    }

                    if (minDiffHash >= 0 && minDiffHash <= 100)
                    {
                        value = CoenM.ImageHash.CompareHash.Similarity(index.Hashes.DifferenceHash, src.Hashes.DifferenceHash);
                        if (value >= minDiffHash)
                            return true;
                    }

                    if (minPerHash >= 0 && minPerHash <= 100)
                    {
                        value = CoenM.ImageHash.CompareHash.Similarity(index.Hashes.PerceptualHash, src.Hashes.PerceptualHash);
                        if (value >= minPerHash)
                            return true;
                    }

                    return false;
                });

            if (skip > 0)
                result = result.Skip(skip);

            if (take > 0)
                result = result.Take(take);

            return result;
        }

        public IEnumerable<ImageData> FindByContentHash(byte[] imageHash)
        {
            Dawn.Guard.Argument(imageHash, nameof(imageHash)).NotNull();
            return Find(index => index.Hashes.ImageHash.SequenceEqual(imageHash));
        }

        public IEnumerable<ImageData> FindImageHashesNotInList(IEnumerable<byte[]> imageHashes)
        {
            Dawn.Guard.Argument(imageHashes, nameof(imageHashes)).NotNull();

            var hashes = imageHashes.ToArray();

            if (!hashes.Any())
                return Enumerable.Empty<ImageData>();

            return Find(index => !hashes.Contains(index.Hashes.ImageHash));
        }

        public int Count(Predicate<ImageData> predicate)
        {
            Dawn.Guard.Argument(predicate, nameof(predicate)).NotNull();

            return data.Count(index => predicate(index));
        }

        public int CountSimilar(ImageData src, double minAvgHash = 95, double minDiffHash = 95, double minPerHash = 95)
        {
            return FindSimilar(src, minAvgHash, minDiffHash, minPerHash).Count();
        }

        public void Delete(ImageData item)
        {
            Dawn.Guard.Argument(item, nameof(item)).NotNull();

            lock (syncLock)
            {
                var existingItem = data.FirstOrDefault(index => index.Identifier.Equals(item.Identifier, StringComparison.InvariantCulture));

                if (existingItem == null)
                    return;

                data.Remove(existingItem);
                storage.Save(data);
            }
        }

        public void AddOrUpdate(ImageData item)
        {
            Dawn.Guard.Argument(item, nameof(item)).NotNull();

            lock (syncLock)
            {
                var existingItem = data.FirstOrDefault(index => index.Identifier.Equals(item.Identifier, StringComparison.InvariantCulture));

                if (existingItem != null)
                    data.Remove(existingItem);

                data.Add(item);
                storage.Save(data);
            }
        }
    }
}
