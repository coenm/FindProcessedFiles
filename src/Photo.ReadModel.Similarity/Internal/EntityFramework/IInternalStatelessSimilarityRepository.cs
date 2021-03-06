﻿namespace EagleEye.Photo.ReadModel.Similarity.Internal.EntityFramework
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using EagleEye.Photo.ReadModel.Similarity.Internal.EntityFramework.Models;
    using JetBrains.Annotations;

    internal interface IInternalStatelessSimilarityRepository
    {
        [Pure]
        [NotNull]
        Task<HashIdentifiers[]> GetAllHashIdentifiersAsync([NotNull] ISimilarityDbContext db);

        [Pure] [NotNull] HashIdentifiers GetOrAddHashIdentifier([NotNull] ISimilarityDbContext db, [NotNull] string identifier);

        [Pure] [CanBeNull] HashIdentifiers GetHashIdentifier([NotNull] ISimilarityDbContext db, [NotNull] string identifier);

        Task<HashIdentifiers> GetAddHashIdentifierAsync([NotNull] ISimilarityDbContext db, [NotNull] string messageHashIdentifier, CancellationToken ct = default);

        Task<List<PhotoHash>> GetPhotoHashesUntilVersionAsync([NotNull] ISimilarityDbContext db, Guid messageId, [NotNull] HashIdentifiers hashIdentifier, int messageVersion, CancellationToken ct = default);

        [Pure] [CanBeNull] PhotoHash GetPhotoHashByIdAndHashIdentifier([NotNull] ISimilarityDbContext db, Guid photoHashId, [NotNull] HashIdentifiers hashIdentifier);

        Task<PhotoHash> TryGetPhotoHashByIdAndHashIdentifierAsync([NotNull] ISimilarityDbContext db, Guid messageId, [NotNull] HashIdentifiers hashIdentifier, CancellationToken ct);

        [NotNull] List<Scores> GetHashScoresByIdAndBeforeVersion([NotNull] ISimilarityDbContext db, int hashIdentifierId, Guid id, int version);

        [NotNull] List<PhotoHash> GetPhotoHashesByHashIdentifier([NotNull] ISimilarityDbContext db, [NotNull] HashIdentifiers hashIdentifier);

        [NotNull] List<Scores> GetOutdatedScores(ISimilarityDbContext db, Guid photoId, HashIdentifiers hashIdentifier, int version);

        void DeleteScores([NotNull] ISimilarityDbContext db, [NotNull] IEnumerable<Scores> scores);

        IQueryable<Scores> GetScoresForPhotoAndHashIdentifier([NotNull] ISimilarityDbContext db, Guid photoId, [NotNull] HashIdentifiers hashIdentifier);
    }
}
