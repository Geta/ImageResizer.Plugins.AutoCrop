﻿using CommandLine;
using ImageResizer.Configuration;
using ImageResizer.Plugins.AutoCrop.Models;
using ImageResizer.Plugins.FastScaling;
using ImageResizer.Plugins.MozJpeg;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageResizer.Plugins.AutoCrop.Automator
{
    public class Options
    {
        [Option('i', "input", Required = true, HelpText = "Path to search recursively.")]
        public string Input { get; set; }

        [Option('o', "output", Required = true, HelpText = "Path to output to.")]
        public string Output { get; set; }

        [Option('t', "tolerance", Required = false, Default = 35, HelpText = "Color difference tolerance (0-255).")]
        public int Tolerance { get; set; }

        [Option('x', "padx", Required = false, Default = 0, HelpText = "Padding X (0-100).")]
        public int PadX { get; set; }

        [Option('y', "pady", Required = false, Default = 0, HelpText = "Padding Y (0-100).")]
        public int PadY { get; set; }

        [Option('w', "width", Required = false, HelpText = "Max width of output images.")]
        public int? Width { get; set; }

        [Option('h', "height", Required = false, HelpText = "Max height of output images.")]
        public int? Height { get; set; }

        [Option('d', "debug", Required = false, Default = false, HelpText = "Visualizes crop data on images.")]
        public bool Debug { get; set; }

        [Option('m', "mode", Required = false, Default = FitMode.None, HelpText = "Fit mode of the image (Max, Pad, Crop, Carve or Stretch).")]
        public FitMode Mode { get; set; }

        [Option('c', "cropMode", Required = false, Default = FitMode.Pad, HelpText = "Fit mode if crop is success (Max, Pad, Crop, Carve or Stretch).")]
        public FitMode CropMode { get; set; }

        [Option('e', "cropMethod", Required = false, Default = AutoCropMethod.Tolerance, HelpText = "Auto crop method to use (Tolerance or Edge).")]
        public AutoCropMethod CropMethod { get; set; }

        [Option('q', "quality", Required = false, Default = 90, HelpText = "Output quality (0-100).")]
        public int Quality { get; set; }

        [Option('a', "compositeAlpha", Required = false, Default = true, HelpText = "Use alpha channel compositing (slower).")]
        public bool CompositeAlpha { get; set; }

        [Option('s', "sharpen", Required = false, Default = 0, HelpText = "Amount to sharpen (0-100).")]
        public int Sharpen { get; set; }

        [Option('p', "threads", Required = false, Default = 4, HelpText = "Threads to use.")]
        public int Threads { get; set; }
    }

    class Program
    {
        static readonly IPlugin[] _plugins = new IPlugin[]
        {
            new AutoCropPlugin(),
            new FastScalingPlugin(),
            new MozJpegPlugin(),
        };

        static readonly string[] _extensions = new[] 
        { 
            ".jpg", ".jpeg", ".png"
        };

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                          .WithParsed(Run)
                          .WithNotParsed(Abort);
        }

        static void Run(Options options)
        {
            try
            {
                EnsurePath(options.Input);
                EnsurePathCreated(options.Output);

                var configuration = Config.Current;
                InitializeResizer(configuration);

                var instructions = GetResizeInstructions(options);
                var files = GetFiles(options.Input);
                
                var processed = 0;
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = options.Threads,
                };
                
                Parallel.ForEach(files, parallelOptions, path =>
                {
                    var source = path.Substring(options.Input.Length);
                    var fileName = Path.GetFileName(source);
                    var destination = Path.Combine(options.Output, fileName);
                    var job = new ImageJob(path, destination, instructions);

                    try
                    {
                        configuration.Build(job);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing '{source}', {ex.Message}");
                        return;
                    }

                    var percentage = (int)Math.Round(++processed / (double)files.Length * 100);

                    Console.WriteLine($"{percentage}% ({processed} / {files.Length}), processed '{source}'");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal exception: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        static void Abort(IEnumerable<Error> errors)
        {
            foreach (var error in errors)
            {
                Console.WriteLine(error.Tag);
            }
        }

        static Instructions GetResizeInstructions(Options options)
        {
            var collection = new NameValueCollection
            {
                { "autocrop", $"{options.PadX};{options.PadY};{options.Tolerance}" },
                { "autoCropMode", options.CropMode.ToString() },
                { "autoCropMethod", options.CropMethod.ToString() },
                { "quality", options.Quality.ToString() },
                { "fastscale", "true" },
                { "down.filter", "CubicSharp" },
                { "down.speed", "-2" },
                { "scale", "both" }
            };

            if (options.Width.HasValue)
                collection.Add("width", options.Width.ToString());

            if (options.Height.HasValue)
                collection.Add("height", options.Height.ToString());

            if (options.Mode != FitMode.None)
                collection.Add("mode", options.Mode.ToString());

            if (options.Sharpen > 0)
               collection.Add("f.sharpen", options.Sharpen.ToString());

            if (!options.CompositeAlpha)
                collection.Add("f.ignorealpha", "true");
            
            if (options.Debug)
                collection.Add("autocropdebug", "1");

            return new Instructions(collection);
        }

        static void InitializeResizer(Config config)
        {
            foreach (var plugin in _plugins)
            {
                plugin.Install(config);
            }
        }

        static string[] GetFiles(string path)
        {
            var filter = new HashSet<string>(_extensions, StringComparer.InvariantCultureIgnoreCase);

            return Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                            .Where(x => filter.Contains(Path.GetExtension(x)))
                            .ToArray();
        }

        static void EnsurePathCreated(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        static void EnsurePath(string path)
        {
            if (!Directory.Exists(path))
                throw new IOException($"Path '{path}' does not exist");
        }
    }
}
