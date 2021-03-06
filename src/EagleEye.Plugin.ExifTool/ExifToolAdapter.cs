﻿namespace EagleEye.ExifTool
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using CoenM.ExifToolLib;
    using Dawn;
    using JetBrains.Annotations;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NLog;

    internal class ExifToolAdapter : IExifToolReader, IExifToolWriter, IDisposable
    {
        private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly AsyncExifTool exiftoolImpl;

        public ExifToolAdapter([NotNull] IExifToolConfig config, [CanBeNull] IExifToolArguments arguments)
        {
            Guard.Argument(config, nameof(config)).NotNull();
            var args = arguments?.Arguments?.ToArray();

            var exiftoolConfig = new AsyncExifToolConfiguration(config.ExifToolExe, config.ExifToolConfigFile, Encoding.UTF8, args);
            var logger = new ExifToolLogAdapter();

            exiftoolImpl = logger.IsEnabled()
                               ? new AsyncExifTool(exiftoolConfig, logger)
                               : new AsyncExifTool(exiftoolConfig);

            exiftoolImpl.Initialize();
        }

        public async Task<JObject> GetMetadataAsync(string filename, CancellationToken ct = default)
        {
            var result = await exiftoolImpl.ExecuteAsync(filename, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(result))
                return null;

            try
            {
                var jsonResult = JsonConvert.DeserializeObject(result);
                var jsonArray = jsonResult as JArray;
                if (jsonArray?.Count != 1)
                    return null;

                return jsonArray[0] as JObject;
            }
            catch (Exception e)
            {
                Logger.Error($"Could not process result '{result}'. {e.Message}");
                return null;
            }
        }

        public async Task WriteAsync(string filename, IEnumerable<string> exiftoolArgs, CancellationToken ct = default)
        {
            var args = exiftoolArgs.ToList();
            args.Add(filename);

            _ = await exiftoolImpl.ExecuteAsync(args, ct).ConfigureAwait(false);
        }

        public ValueTask DisposeAsync()
        {
           return exiftoolImpl.DisposeAsync();
        }

        void IDisposable.Dispose()
        {
            exiftoolImpl.DisposeAsync().GetAwaiter().GetResult();
        }
    }
}
