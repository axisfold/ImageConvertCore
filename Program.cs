using CommandLine;
using SkiaSharp;
using System;
using System.IO;

namespace ImageConvertCore
{
    class Program
    {
        class Options
        {
            [Value(0, HelpText = "Source File Name")]
            public string SourceFileName { get; set; }

            [Value(1, HelpText = "Destination File Name")]
            public string DestinationFileName { get; set; }

            [Option("png-level", Required = false, HelpText = "Set compression level for PNG output.", Default = 1)]
            public int PngLevel { get; set; }

            [Option("jpg-quality", Required = false, HelpText = "Set quality level for JPG output.", Default = 72)]
            public int JpgLevel { get; set; }
        }

        static void Main(string[] args)
        {
            var sourceFileName = "";
            var destFileName = "";
            var pngLevel = 1;
            var jpgQuality = 72;
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       sourceFileName = o.SourceFileName;
                       destFileName = o.DestinationFileName;
                       pngLevel = o.PngLevel;
                       jpgQuality = o.JpgLevel;
                   });

            if (sourceFileName == null || destFileName == null) return;

            using (var input = File.OpenRead(sourceFileName))
            {
                using (var inputStream = new SKManagedStream(input))
                {
                    var sourceBitmap = SKBitmap.Decode(inputStream);

                    using (var output = File.Create(destFileName))
                    {
                        if (destFileName.ToLower().EndsWith("png"))
                        {
                            sourceBitmap.PeekPixels().Encode(new SKPngEncoderOptions {
                                ZLibLevel = pngLevel,
                                FilterFlags = SKPngEncoderFilterFlags.Paeth
                            }).AsStream().CopyTo(output);
                            
                        }
                        else if (destFileName.ToLower().EndsWith("jpg") || destFileName.ToLower().EndsWith("jpeg"))
                        {
                            sourceBitmap.PeekPixels().Encode(new SKJpegEncoderOptions
                            {
                                Quality = jpgQuality
                            }).AsStream().CopyTo(output);
                        }
                    }
                }
            }
        }
    }
}
