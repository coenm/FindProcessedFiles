﻿namespace EagleEye.ExifToolWrapper
{
    using System.Threading.Tasks;

    using EagleEye.Core.Interfaces;

    using Newtonsoft.Json.Linq;

    public class ExifToolCacheDecorator : IExifTool
    {
        private readonly IExifTool _exiftool;
        private readonly IDateTimeService _dateTimeService;

        public ExifToolCacheDecorator(IExifTool exiftool, IDateTimeService dateTimeService)
        {
            _exiftool = exiftool;
            _dateTimeService = dateTimeService;
        }

        public Task<JObject> GetMetadataAsync(string filename)
        {
            return _exiftool.GetMetadataAsync(filename);
        }

        public void Dispose()
        {
            _exiftool?.Dispose();
        }
    }
}