﻿namespace EagleEye.Core.DefaultImplementations
{
    using System.IO;

    using EagleEye.Core.Interfaces.Core;

    public class SystemFileService : IFileService
    {
        private SystemFileService()
        {
        }

        public static SystemFileService Instance { get; } = new SystemFileService();

        public Stream OpenRead(string filename) => File.OpenRead(filename);

        public bool FileExists(string filename) => File.Exists(filename);
    }
}
