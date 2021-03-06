﻿namespace EagleEye.Photo.Domain.Commands
{
    using System;

    using Dawn;
    using EagleEye.Photo.Domain.Commands.Base;
    using JetBrains.Annotations;

    [PublicAPI]
    public class UpdateFileHashCommand : CommandBase
    {
        public UpdateFileHashCommand(Guid id, int expectedVersion, [NotNull] byte[] fileHash)
        : base(id, expectedVersion)
        {
            Guard.Argument(fileHash, nameof(fileHash)).NotNull();

            FileHash = fileHash;
        }

        public byte[] FileHash { get; }
    }
}
