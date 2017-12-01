﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CommandLine;
using FileImporter.CmdOptions;
using FileImporter.Data;
using FileImporter.Json;

namespace FileImporter
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Run(args);
        }

        public static void Run(string[] args)
        {
            Parser.Default.ParseArguments<IndexOptions, MergeOptions, FindAndHandleDuplicatesOptions>(args)
                .WithParsed<IndexOptions>(IndexData)
                .WithParsed<MergeOptions>(ProcessMerge)
                .WithParsed<FindAndHandleDuplicatesOptions>(FindAndProcessDuplicates)
                .WithNotParsed(errs => Console.WriteLine("Could not parse the arguments."));

            Console.WriteLine("Done. Press enter to exit.");
            Console.ReadLine();
        }

        private static void FindAndProcessDuplicates(FindAndHandleDuplicatesOptions opts)
        {
            if (string.IsNullOrWhiteSpace(opts.IndexFile))
                ShowError($"Indexfile cannot be null or empty.");

            if (!File.Exists(opts.IndexFile))
                ShowError($"File '{opts.IndexFile}' doesn't exist.");

            if (string.IsNullOrWhiteSpace(opts.ProcessingFile))
                ShowError($"ProcessingFile cannot be null or empty.");

            if (!File.Exists(opts.ProcessingFile))
                ShowError($"File '{opts.ProcessingFile}' doesn't exist.");

            if (opts.DuplicateAction == FileAction.Move)
            {
                if (string.IsNullOrWhiteSpace(opts.DuplicateDir))
                    ShowError($"DuplicateDir cannot be null or empty.");

                if (!Directory.Exists(opts.DuplicateDir))
                    Directory.CreateDirectory(opts.DuplicateDir);
            }


            var index = ReadInputFile(opts.IndexFile);
            var filesToProcess = ReadInputFile(opts.ProcessingFile);


            // find duplicate files
            var duplicateFiles = filesToProcess
                .Where(f => index.Any(file => file.Sha256.SequenceEqual(f.Sha256)))
                .ToArray();

            // find new files
            var newFiles = filesToProcess.Except(duplicateFiles).ToArray();


            HandleDuplicates(duplicateFiles, opts);
            //HandleNewFiles(newFiles, opts);

            JsonEncoding.WriteDateToJsonFile(duplicateFiles, opts.OutputDuplicateFile);
            JsonEncoding.WriteDateToJsonFile(newFiles, opts.OutputNewFile);
        }

        private static void IndexData(IndexOptions opts)
        {
            if (string.IsNullOrWhiteSpace(opts.DirectoryToIndex))
                ShowError("Directory cannot be null or empty.");

            if (!Directory.Exists(opts.DirectoryToIndex))
                ShowError($"Directory '{opts.DirectoryToIndex}' does not exists.");

            if (string.IsNullOrWhiteSpace(opts.OutputFile))
                ShowError("Outputfile cannot be null or empty.");

            var existingData = new List<FileData>();
            if (File.Exists(opts.OutputFile) && opts.AppendResults)
            {
                existingData = ReadInputFile(opts.OutputFile);
            }

            var processedFiles = ProcessDirectory(opts.DirectoryToIndex);
            
            var result = existingData.Concat(processedFiles).ToList();
            JsonEncoding.WriteDateToJsonFile(result, opts.OutputFile);
        }

        private static void ProcessMerge(MergeOptions opts)
        {
            if (string.IsNullOrWhiteSpace(opts.InputFile1))
                ShowError($"Inputfile1 cannot be null or empty.");

            if (!File.Exists(opts.InputFile1))
                ShowError($"File '{opts.InputFile1}' doesn't exist.");

            if (string.IsNullOrWhiteSpace(opts.InputFile2))
                ShowError($"Inputfile2 cannot be null or empty.");

            if (!File.Exists(opts.InputFile2))
                ShowError($"File '{opts.InputFile2}' doesn't exist.");

            if (string.IsNullOrWhiteSpace(opts.OutputFile))
                ShowError("Output file cannot be null or empty.");

            if (File.Exists(opts.OutputFile) && opts.OverwriteOutput == false)
                ShowError($"File '{opts.OutputFile}' already exists and overwrite is disabled.");

            var file1Content = ReadInputFile(opts.InputFile1);
            var file2Content = ReadInputFile(opts.InputFile2);

            JsonEncoding.WriteDateToJsonFile(file1Content.Concat(file2Content).ToList(), opts.OutputFile);
        }

        private static void ShowError(string s)
        {
            Console.WriteLine(s);
            throw new Exception(s);
        }

        private static void HandleDuplicates(FileData[] files, FindAndHandleDuplicatesOptions options)
        {
            if (options.DuplicateAction == FileAction.Keep)
                return;


            if (options.DuplicateAction == FileAction.Delete)
            {
                foreach (var file in files)
                {
                    try
                    {
                        if (File.Exists(file.FileName))
                            File.Delete(file.FileName);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($" Error deleting file '{file.FileName}'. Ex msg : {e.Message}");
                    }
                }
            }


            if (options.DuplicateAction == FileAction.Move)
            {
                foreach (var file in files)
                {
                    try
                    {
                        var fi = new FileInfo(file.FileName);
                        if (fi.Exists)
                        {
                            var destFileName = Path.Combine(options.DuplicateDir, fi.Name);
                            fi.MoveTo(destFileName);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($" Error moving file '{file.FileName}'. Ex msg : {e.Message}");
                    }
                }
            }
        }

        private static IEnumerable<FileData> ProcessDirectory(string inputDir)
        {
            if (!Directory.Exists(inputDir))
                throw new Exception("Directory does not exists.");

            var files = Directory
                .EnumerateFiles(inputDir, "*.*", SearchOption.AllDirectories)
                .Where(f => !f.ToLower().EndsWith(".zip"))
                .Where(f => !f.ToLower().EndsWith(".ini"))
                .ToArray();

            var total = files.Length;
            var result = new List<FileData>(total);

            result.AddRange(files.Select(ProcesFile));

            return result;
        }

        private static FileData ProcesFile(string file)
        {
            try
            {
                var fileinfo = new FileInfo(file);
                var result = new FileData
                {
                    FileName = file,
                    SizeInBytes = fileinfo.Length
                };

                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
                {
                    result.Sha256 = CalculateHash(fileStream);

                }

                return result;
            }
            catch (Exception)
            {
                Console.WriteLine($"Could not procses file '{file}'");
                throw;
            }
        }

        private static List<FileData> ReadInputFile(string filename)
        {
            if (!File.Exists(filename))
                throw new Exception("File doesn't exists.");

            try
            {
                return JsonEncoding.ReadFromFile<List<FileData>>(filename);
            }
            catch (Exception)
            {
                throw new Exception($"Could not read or parse '{filename}'.");
            }
        }

        public static byte[] CalculateHash(Stream stream)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.CanSeek);

            stream.Position = 0;

            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(stream);
            }
        }
    }
}
