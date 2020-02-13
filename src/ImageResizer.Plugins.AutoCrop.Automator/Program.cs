using CommandLine;
using ImageResizer.Configuration;
using ImageResizer.Plugins.FastScaling;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

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

        [Option('m', "mode", Required = false, Default = FitMode.None, HelpText = "Fit mode of the image (Max, Pad, Crop, Carve or Stretch).")]
        public FitMode Mode { get; set; }
    }

    class Program
    {
        static IPlugin[] _plugins = new IPlugin[]
        {
            new AutoCropPlugin(),
            new FastScalingPlugin()
        };

        static string[] _extensions = new[] 
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
                
                foreach (var source in files)
                {
                    var fileName = Path.GetFileName(source);
                    var destination = Path.Combine(options.Output, fileName);
                    var job = new ImageJob(source, destination, instructions);

                    try
                    {
                        configuration.Build(job);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing '{fileName}', {ex.Message}");
                        continue;
                    }

                    var percentage = (int)Math.Round(++processed / (double)files.Length * 100);

                    Console.WriteLine($"{percentage}% ({processed} / {files.Length}), processed '{fileName}'");
                }
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
                { "autoCrop", $"{options.PadX};{options.PadY};{options.Tolerance}" },
                { "fastscale", "true" },
                { "scale", "both" }
            };

            if (options.Width.HasValue)
                collection.Add("width", options.Width.ToString());

            if (options.Height.HasValue)
                collection.Add("height", options.Height.ToString());

            if (options.Mode != FitMode.None)
                collection.Add("mode", options.Mode.ToString());

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
