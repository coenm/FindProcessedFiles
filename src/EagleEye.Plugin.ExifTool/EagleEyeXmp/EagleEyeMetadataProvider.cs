﻿namespace EagleEye.ExifTool.EagleEyeXmp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Dawn;
    using EagleEye.Core.EagleEyeXmp;
    using EagleEye.Core.Interfaces.PhotoInformationProviders;
    using EagleEye.ExifTool.Parsing;
    using JetBrains.Annotations;
    using Newtonsoft.Json.Linq;

    internal class EagleEyeMetadataProvider : IEagleEyeMetadataProvider
    {
        private readonly IExifToolReader exiftool;

        public EagleEyeMetadataProvider([NotNull] IExifToolReader exiftool)
        {
            Guard.Argument(exiftool, nameof(exiftool)).NotNull();
            this.exiftool = exiftool;
        }

        public string Name => nameof(EagleEyeMetadataProvider);

        public uint Priority { get; } = 100;

        public bool CanProvideInformation(string filename) => !string.IsNullOrWhiteSpace(filename);

        public async Task<EagleEyeMetadata> ProvideAsync(string filename, CancellationToken ct = default)
        {
            var resultExiftool = await exiftool.GetMetadataAsync(filename, ct).ConfigureAwait(false);

            if (resultExiftool == null)
                return null;

            return GetInformationFromFullJsonObject(resultExiftool);
        }

        [CanBeNull]
        [Pure]
        private static byte[] TryGetZ85Bytes([NotNull] JObject data, [NotNull] string key)
        {
            var s = TryGetString(data, key);
            if (s == null)
                return null;

            try
            {
                return CoenM.Encoding.Z85.Decode(s);
            }
            catch (Exception)
            {
                return null;
            }
        }

        [CanBeNull]
        [Pure]
        private EagleEyeMetadata GetInformationFromFullJsonObject(JObject data)
        {
            if (!(data["XMP"] is JObject headerObject))
                return null;

            var version = TryGetString(headerObject, "EagleEyeVersion");
            if (string.IsNullOrWhiteSpace(version))
                return null;

            if (!version.Equals("1"))
                return null;

            var result = new EagleEyeMetadata();

            if (headerObject["EagleEyeTimestamp"] is { } token)
            {
                var dt = GetDateTimeFromJToken(token);
                if (dt.HasValue)
                    result.Timestamp = dt.Value;
            }

            var guidBytes = TryGetZ85Bytes(headerObject, "EagleEyeId");
            if (guidBytes != null)
                result.Id = new Guid(guidBytes);

            var fileHashBytes = TryGetZ85Bytes(headerObject, "EagleEyeFileHash");
            if (fileHashBytes != null)
                result.FileHash = fileHashBytes;

            if (headerObject["EagleEyeRawImageHash"] is { } rawImageHashToken)
            {
                if (rawImageHashToken.Type == JTokenType.Array)
                {
                    var rawImageHashes = new List<string>(rawImageHashToken.Count());
                    foreach (var item in rawImageHashToken.Values<string>())
                    {
                        if (!string.IsNullOrWhiteSpace(item))
                            rawImageHashes.Add(item);
                    }

                    foreach (var z85Encoded in rawImageHashes.Distinct().Where(x => !string.IsNullOrWhiteSpace(x)))
                    {
                        try
                        {
                            result.RawImageHash.Add(CoenM.Encoding.Z85.Decode(z85Encoded));
                        }
                        catch (Exception)
                        {
                            // ignore
                        }
                    }
                }
            }

            return result;
        }

        [CanBeNull]
        [Pure]
        private static string TryGetString([NotNull] JObject data, [NotNull] string key)
        {
            if (!(data[key] is { } token))
                return null;

            if (token.Type == JTokenType.String)
                return token.Value<string>();

            if (token.Type == JTokenType.Integer)
                return token.Value<int>().ToString();

            return null;
        }

        [CanBeNull]
        [Pure]
        private static DateTime? GetDateTimeFromJToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Date:
                    return token.Value<DateTime>();

                case JTokenType.String:
                    var dateTimeString = token.Value<string>();
                    return DateTimeParsing.ParseFullDate(dateTimeString);
            }

            return null;
        }
    }
}
