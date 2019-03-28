﻿namespace EagleEye.Photo.Domain.Services
{
    using System.Collections.Generic;

    using Helpers.Guards; using Dawn;
    using JetBrains.Annotations;

    internal class UniqueFilenameService : IUniqueFilenameService
    {
        [NotNull] private readonly object syncLock = new object();
        [NotNull] private readonly IFilenameRepository repository;
        [NotNull] private readonly List<string> claimedFileNames;

        public UniqueFilenameService([NotNull] IFilenameRepository repository)
        {
            Dawn.Guard.Argument(repository, nameof(repository)).NotNull();
            this.repository = repository;
            claimedFileNames = new List<string>();
        }

        [CanBeNull]
        public IClaimFilenameToken Claim([NotNull] string filename)
        {
            Dawn.Guard.Argument(filename, nameof(filename)).NotNull().NotEmpty();

            lock (syncLock)
            {
                if (repository.Contains(filename))
                    return null;

                if (claimedFileNames.Contains(filename))
                    return null;

                claimedFileNames.Add(filename);

                return new ClaimFilenameToken(this, filename);
            }
        }

        private void RemoveClaim([NotNull] string filename)
        {
            Dawn.Guard.Argument(filename, nameof(filename)).NotNull();

            lock (syncLock)
            {
                claimedFileNames.Remove(filename);
            }
        }

        private void CommitClaim([NotNull] string filename)
        {
            Dawn.Guard.Argument(filename, nameof(filename)).NotNull();

            lock (syncLock)
            {
                repository.Add(filename);
            }
        }

        private class ClaimFilenameToken : IClaimFilenameToken
        {
            [NotNull] private readonly UniqueFilenameService parent;
            [NotNull] private readonly string filename;

            public ClaimFilenameToken([NotNull] UniqueFilenameService parent, [NotNull] string filename)
            {
                Dawn.Guard.Argument(parent, nameof(parent)).NotNull();
                Dawn.Guard.Argument(filename, nameof(filename)).NotNull();
                this.parent = parent;
                this.filename = filename;
            }

            public void Commit() => parent.CommitClaim(filename);

            public void Dispose() => parent.RemoveClaim(filename);
        }
    }
}
