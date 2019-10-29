using CommandLine;
using SkiaSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using TurboJpegWrapper;

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

            [Option("turbo", Required = false, HelpText = "Use libjpeg-turbo", Default = false)]
            public bool Turbo { get; set; }
        }

        static void Main(string[] args)
        {
            var sourceFileName = "";
            var destFileName = "";
            var pngLevel = 1;
            var jpgQuality = 72;
            var turbo = false;
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       sourceFileName = o.SourceFileName;
                       destFileName = o.DestinationFileName;
                       pngLevel = o.PngLevel;
                       jpgQuality = o.JpgLevel;
                       turbo = o.Turbo;
                   });

            if (sourceFileName == null || destFileName == null) return;

            if (turbo && (destFileName.ToLower().EndsWith("jpg") || destFileName.ToLower().EndsWith("jpeg")))
            {
                var bmp = (Bitmap)Image.FromFile(sourceFileName);
                var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly,
                        bmp.PixelFormat);
                var _compressor = new TJCompressor();
                var options = TJSubsamplingOptions.TJSAMP_420;
                var result = _compressor.Compress(data.Scan0, data.Stride, data.Width, data.Height, ConvertPixelFormat(data.PixelFormat), options, jpgQuality, TJFlags.NONE);

                File.WriteAllBytes(destFileName, result);

                return;
            }

            using (var input = File.OpenRead(sourceFileName))
            {
                using (var inputStream = new SKManagedStream(input))
                {
                    var sourceBitmap = SKBitmap.Decode(inputStream);

                    using (var output = File.Create(destFileName))
                    {
                        if (destFileName.ToLower().EndsWith("png"))
                        {
                            sourceBitmap.PeekPixels().Encode(new SKPngEncoderOptions
                            {
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

        public static TJPixelFormats ConvertPixelFormat(PixelFormat pixelFormat)
        {
            switch (pixelFormat)
            {
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                    return TJPixelFormats.TJPF_BGRA;
                case PixelFormat.Format24bppRgb:
                    return TJPixelFormats.TJPF_BGR;
                case PixelFormat.Format8bppIndexed:
                    return TJPixelFormats.TJPF_GRAY;
                default:
                    throw new NotSupportedException(string.Format("Provided pixel format \"{0}\" is not supported", pixelFormat));
            }
        }
    }
}
