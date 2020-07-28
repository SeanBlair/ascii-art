using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ascii_art
{
    internal static class Program
    {
        // The different ways to compute a pixel's brightness level.
        private enum BrightnessType
        {
            Average,
            MinMax,
            Luminosity
        }

        // Max number of pixels displayable in single line on my machine's command line app.
        private const int MaxWidthPixels = 105;
        // Max value of a pixel's Red, Green or Blue value.
        private const int MaxPixelBrightness = 255;
        private const BrightnessType DefaultBrightnessType = BrightnessType.Luminosity;
        private const bool DefaultInvertBrightness = true;
        // Command line args indexes.
        private const int ImagePathArgIndex = 0;
        private const int WidthPixelsArgIndex = 1;
        private const int BrightnessTypeArgIndex = 2;
        private const int InvertBrightnessArgIndex = 3;
        // Program settings.
        private static string imagePath;
        private static int widthPixels;
        private static BrightnessType brightnessType;
        private static bool invertBrightness;
        

        private static void Main(string[] args)
        {
            try
            {
                ProcessCommandLineArgs(args);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Processing the program's command line arguments!");
                Console.WriteLine(e);
                DisplayCommandLineArgumentsInstructions();
                return;
            }

            DisplayProgramSettings();

            Rgb24[][] pixelMatrix;

            try
            {
                pixelMatrix = BuildPixelMatrix();
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n Error reading image = {imagePath}!!");
                Console.WriteLine(e);
                DisplayCommandLineArgumentsInstructions();
                return;
            }
            
            var brightnessMatrix = BuildBrightnessMatrix(pixelMatrix);

            var asciiMatrix = BuildAsciiMatrix(brightnessMatrix);

            DisplayAsciiArt(asciiMatrix);
        }

        private static void ProcessCommandLineArgs(IReadOnlyList<string> args)
        {
            if (args.Count == 0)
            {
                throw new ArgumentException("No file specified!");
            }
            imagePath = args[ImagePathArgIndex];

            // Optional command line arguments.
            widthPixels = args.Count > 1 ? GetWidthPixelsArg(args) : MaxWidthPixels;
            brightnessType = args.Count > 2 ? GetBrightnessTypeArg(args) : DefaultBrightnessType;
            invertBrightness = args.Count > 3 ? GetInvertBrightnessArg(args) : DefaultInvertBrightness;
        }

        private static bool GetInvertBrightnessArg(IReadOnlyList<string> args)
        {
            if (!bool.TryParse(args[InvertBrightnessArgIndex], out var invertBrightnessArg))
            {
                throw new ArgumentException("Unknown Invert Brightness value, must be either true or false!");
            }

            return invertBrightnessArg;
        }

        private static BrightnessType GetBrightnessTypeArg(IReadOnlyList<string> args)
        {
            if (!Enum.TryParse<BrightnessType>(args[BrightnessTypeArgIndex], out var brightnessTypeValue))
            {
                throw new ArgumentException($"Unknown brightness type: should be one of: {BrightnessType.Average}" +
                                            $", {BrightnessType.MinMax} or {BrightnessType.Luminosity}");    
            }
            return brightnessTypeValue;
        }

        private static void DisplayAsciiArt(IEnumerable<string> asciiMatrix)
        {
            foreach (var asciiRow in asciiMatrix)
            {
                Console.WriteLine(asciiRow);
            }
        }

        private static Rgb24[][] BuildPixelMatrix()
        {
            using var image = Image.Load<Rgb24>(imagePath);
            
            image.Mutate(x => x
                .Resize(new ResizeOptions()
                {
                    Mode = ResizeMode.Max, 
                    Size = new Size(widthPixels)
                }));
        
            return BuildPixelMatrix(image);
        }

        private static void DisplayProgramSettings()
        {
            Console.WriteLine($"Image = {imagePath}");
            Console.WriteLine($"Width (pixels) = {widthPixels}");
            Console.WriteLine($"Brightness Type = {brightnessType}");
            Console.WriteLine($"Invert Brightness = {invertBrightness}");
        }

        private static int GetWidthPixelsArg(IReadOnlyList<string> args)
        {
            if (!int.TryParse(args[WidthPixelsArgIndex], out var widthPixelsArg))
            {
                throw new ArgumentException("Error reading the Width Pixels argument = " +
                                            $"{args[WidthPixelsArgIndex]}! It must be a number!");
            }
            if (widthPixelsArg < 0 || widthPixelsArg > MaxWidthPixels)
            {
                throw new ArgumentException($"Width pixels argument must be between 0 and {MaxWidthPixels}!");
            }

            return widthPixelsArg;
        }

        private static void DisplayCommandLineArgumentsInstructions()
        {
            Console.WriteLine("\nAll Program Arguments:");
            Console.WriteLine("1) Path to source image.");
            Console.WriteLine("2) Max width (pixels). Default = 105");
            Console.WriteLine("3) Brightness Type: Default = Luminosity");
            Console.WriteLine("  a) Average: (R + G + B) / 3");
            Console.WriteLine("  b) MinMax: (Min(R,G,B) + Max(R,G,B) / 2)");
            Console.WriteLine("  c) Luminosity: (R * 0.21 + G * 0.72 + B * 0.07) Default") ;
            Console.WriteLine("4) Invert Brightness: true/false. Default = true");
        }

        private static IEnumerable<string> BuildAsciiMatrix(IReadOnlyList<int[]> brightnessMatrix)
        {
            var asciiMatrix = new string[brightnessMatrix.Count];

            for (var i = 0; i < brightnessMatrix.Count; i++)
            {
                asciiMatrix[i] = string.Empty;
                for (var j = 0; j < brightnessMatrix[0].Length; j++)
                {
                    asciiMatrix[i] += GetAsciiCharactersForBrightness(brightnessMatrix[i][j]);
                }
            }

            return asciiMatrix;
        }

        private static string GetAsciiCharactersForBrightness(int brightness)
        {
            const string asciiCharsOrderedByBrightness = "`^\",:;Il!i~+_-?][}{1)(|\\/tfjrxnuvczXYUJCLQ0OZmwqpdbkhao*#MW&8%B@$";
            var maxAsciiCharsIndex = asciiCharsOrderedByBrightness.Length - 1;

            // Get the index proportional to the given brightness. (brightness is to maxPixelBrightness as asciiCharIndex
            // is to maxAsciiCharIndex).
            var asciiCharIndex = brightness * maxAsciiCharsIndex / MaxPixelBrightness;

            var asciiChar = asciiCharsOrderedByBrightness[asciiCharIndex].ToString();
            
            // Append two characters to make the resulting string relatively square. This is because pixels are square
            // and ascii symbols rectangular (roughly twice as high as they are wide), and otherwise the resulting image
            // will appear squashed and narrow.
            return asciiChar + asciiChar;
        }

        private static int[][] BuildBrightnessMatrix(IReadOnlyList<Rgb24[]> pixelMatrix)
        {
            var pixelRowLength = pixelMatrix[0].Length;
            var brightnessMatrix = new int[pixelMatrix.Count][];
            for (var i = 0; i < pixelMatrix.Count; i++)
            {
                brightnessMatrix[i] = new int[pixelRowLength];
                for (var j = 0; j < pixelRowLength; j++)
                {
                    brightnessMatrix[i][j] = ComputePixelBrightness(pixelMatrix[i][j]);
                }
            }

            return brightnessMatrix;
        }

        private static int ComputePixelBrightness(Rgb24 pixel)
        {
            var pixelBrightness = ComputeConfiguredPixelBrightness(pixel);

            if (!invertBrightness) return pixelBrightness;
            
            // Invert pixel brightness
            pixelBrightness = MaxPixelBrightness - pixelBrightness;

            return pixelBrightness;
        }

        private static int ComputeConfiguredPixelBrightness(Rgb24 pixel)
        {
            switch (brightnessType)
            {
                case BrightnessType.Average:
                    return (pixel.R + pixel.G + pixel.B) / 3;
                case BrightnessType.MinMax:
                {
                    var min = Math.Min(Math.Min(pixel.R, pixel.G), pixel.B);
                    var max = Math.Max(Math.Max(pixel.R, pixel.G), pixel.B);

                    return (min + max) / 2;
                }
                case BrightnessType.Luminosity:
                {
                    const double redWeight = 0.21;
                    const double greenWeight = 0.72;
                    const double blueWeight = 0.07;
                
                    return (int) (pixel.R * redWeight + pixel.G * greenWeight + pixel.B * blueWeight);
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static Rgb24[][] BuildPixelMatrix(Image<Rgb24> image)
        {
            var pixelMatrix = new Rgb24[image.Height][];
        
            for (var i = 0; i < pixelMatrix.Length; i++)
            {
                pixelMatrix[i] = image.GetPixelRowSpan(i).ToArray();
            }
        
            return pixelMatrix;
        }
    }
}
