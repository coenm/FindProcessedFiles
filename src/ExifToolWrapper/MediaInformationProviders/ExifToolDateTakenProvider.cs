﻿[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ExifToolWrapper.Test")]

namespace EagleEye.ExifToolWrapper.MediaInformationProviders
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;

    using EagleEye.Core;
    using EagleEye.Core.Interfaces;
    using EagleEye.ExifToolWrapper.MediaInformationProviders.Parsing;

    using JetBrains.Annotations;

    using Newtonsoft.Json.Linq;

    public class ExifToolDateTakenProvider : IMediaInformationProvider
    {
        private static readonly List<MetadataHeaderKeyPair> _keys = new List<MetadataHeaderKeyPair>
                                                                        {
                                                                            new MetadataHeaderKeyPair(MetadataHeaderKeyPair.Keys.EXIF, MetadataHeaderKeyPair.Keys.EXIF_IFD, "DateTimeOriginal"),
                                                                            new MetadataHeaderKeyPair(MetadataHeaderKeyPair.Keys.XMP, MetadataHeaderKeyPair.Keys.XMP_EXIF, "DateTimeOriginal"),
                                                                            new MetadataHeaderKeyPair(MetadataHeaderKeyPair.Keys.COMPOSITE, MetadataHeaderKeyPair.Keys.COMPOSITE, "SubSecDateTimeOriginal"),
                                                                            new MetadataHeaderKeyPair(MetadataHeaderKeyPair.Keys.XMP, MetadataHeaderKeyPair.Keys.XMP_EXIF, "DateTimeDigitized"),
                                                                            new MetadataHeaderKeyPair(MetadataHeaderKeyPair.Keys.COMPOSITE, MetadataHeaderKeyPair.Keys.COMPOSITE, "SubSecCreateDate"),
                                                                        };

        private readonly IExifTool _exiftool;

        public ExifToolDateTakenProvider(IExifTool exiftool)
        {
            _exiftool = exiftool;
        }

        public int Priority { get; } = 100;

        public bool CanProvideInformation(string filename)
        {
            return true;
        }

        public async Task ProvideAsync(string filename, MediaObject media)
        {
            var result = await _exiftool.GetMetadataAsync(filename).ConfigureAwait(false);

            if (result == null)
                return;

            var dateTimeTaken = GetDateTimeFromFullJsonObject(result);

            if (dateTimeTaken != null)
                media.SetDateTimeTaken(dateTimeTaken);
        }


        // ReSharper disable once MemberCanBePrivate.Global
        // Method is public for unittest purposes.
        // Improvment by extracting to new (static?) class
        [Pure]
        internal static DateTime? ParseFullDate(string data)
        {
            if (DateTimeOffset.TryParseExact(data, "yyyy:MM:dd HH:mm:ss", null, DateTimeStyles.None, out var dateTimeOffset))
                return dateTimeOffset.DateTime;

            if (DateTimeOffset.TryParseExact(data, "yyyy-MM-dd HH:mm:ss", null, DateTimeStyles.None, out dateTimeOffset))
                return dateTimeOffset.DateTime;

            if (DateTimeOffset.TryParseExact(data, "yyyy:MM:dd HH:mm:sszzz", null, DateTimeStyles.None, out dateTimeOffset))
                return dateTimeOffset.DateTime;

            if (DateTimeOffset.TryParseExact(data, "yyyy-MM-dd HH:mm:sszzz", null, DateTimeStyles.None, out dateTimeOffset))
                return dateTimeOffset.DateTime;

            if (DateTimeOffset.TryParseExact(data, "yyyy:MM:dd HH:mm:sszz", null, DateTimeStyles.None, out dateTimeOffset))
                return dateTimeOffset.DateTime;

            if (DateTimeOffset.TryParseExact(data, "yyyy-MM-dd HH:mm:sszz", null, DateTimeStyles.None, out dateTimeOffset))
                return dateTimeOffset.DateTime;

            if (DateTimeOffset.TryParseExact(data, "yyyy:MM:dd HH:mm:ssz", null, DateTimeStyles.None, out dateTimeOffset))
                return dateTimeOffset.DateTime;

            if (DateTimeOffset.TryParseExact(data, "yyyy-MM-dd HH:mm:ssz", null, DateTimeStyles.None, out dateTimeOffset))
                return dateTimeOffset.DateTime;

            return null;
        }

        [CanBeNull, Pure]
        private static Timestamp GetDateTimeFromFullJsonObject(JObject data)
        {
            foreach (var header in _keys)
            {
                var result = GetDateTimeFromKeyKeyPair(data, header.Header1, header.Key);
                if (result != null)
                    return result;

                result = GetDateTimeFromKeyKeyPair(data, header.Header2, header.Key);
                if (result != null)
                    return result;
            }

            return null;
        }

        [CanBeNull, Pure]
        private static Timestamp GetDateTimeFromKeyKeyPair([NotNull] JObject data, [NotNull] string header, [NotNull] string key)
        {
            if (!(data[header] is JObject headerObject))
                return null;

            if (!(headerObject[key] is JToken token))
                return null;

            return GetDateTimeFromJToken(token);
        }

        [CanBeNull, Pure]
        private static Timestamp GetDateTimeFromJToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Date:
                    var datetime = token.Value<DateTime>();
                    return Timestamp.FromDateTime(datetime);

                case JTokenType.String:
                    var dateTimeString = token.Value<string>();
                    var dt = ParseFullDate(dateTimeString);
                    if (dt == null)
                        return null;
                    return Timestamp.FromDateTime(dt.Value);
            }

            return null;
        }
    }
}