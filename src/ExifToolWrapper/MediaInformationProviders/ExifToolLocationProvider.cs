﻿namespace EagleEye.ExifToolWrapper.MediaInformationProviders
{
    using System.Threading.Tasks;

    using EagleEye.Core;
    using EagleEye.Core.Data;
    using EagleEye.Core.Interfaces;
    using EagleEye.Core.Interfaces.PhotoInformationProviders;

    using Helpers.Guards;
    using JetBrains.Annotations;
    using Newtonsoft.Json.Linq;

    public class ExifToolLocationProvider : IMediaInformationProvider
    {
        private readonly IExifTool exiftool;

        public ExifToolLocationProvider([NotNull] IExifTool exiftool)
        {
            Guard.NotNull(exiftool, nameof(exiftool));
            this.exiftool = exiftool;
        }

        public uint Priority { get; } = 100;

        public bool CanProvideInformation(string filename)
        {
            return true;
        }

        public async Task ProvideAsync(string filename, MediaObject media)
        {
            var result = await exiftool.GetMetadataAsync(filename).ConfigureAwait(false);

            if (result == null)
                return;

            string s;

            if (result["XMP"] is JObject data)
            {
                s = TryGetString(data, "CountryCode");
                if (!string.IsNullOrWhiteSpace(s))
                    media.Location.CountryCode = s;

                s = TryGetString(data, "Location");
                if (!string.IsNullOrWhiteSpace(s))
                    media.Location.SubLocation = s;

                s = TryGetString(data, "Country");
                if (!string.IsNullOrWhiteSpace(s))
                    media.Location.CountryName = s;

                s = TryGetString(data, "State");
                if (!string.IsNullOrWhiteSpace(s))
                    media.Location.State = s;

                s = TryGetString(data, "City");
                if (!string.IsNullOrWhiteSpace(s))
                    media.Location.City = s;
            }

            data = result["XMP-iptcCore"] as JObject;
            if (data != null)
            {
                s = TryGetString(data, "CountryCode");
                if (!string.IsNullOrWhiteSpace(s))
                    media.Location.CountryCode = s;

                s = TryGetString(data, "Location");
                if (!string.IsNullOrWhiteSpace(s))
                    media.Location.SubLocation = s;
            }

            data = result["XMP-photoshop"] as JObject;
            if (data != null)
            {
                s = TryGetString(data, "Country");
                if (!string.IsNullOrWhiteSpace(s))
                    media.Location.CountryName = s;

                s = TryGetString(data, "State");
                if (!string.IsNullOrWhiteSpace(s))
                    media.Location.State = s;

                s = TryGetString(data, "City");
                if (!string.IsNullOrWhiteSpace(s))
                    media.Location.City = s;
            }
        }

        private string TryGetString([NotNull] JObject data, [NotNull] string key)
        {
            if (!(data[key] is JToken token))
                return null;

            if (token.Type == JTokenType.String)
                return token.Value<string>();

            return null;
        }
    }
}
