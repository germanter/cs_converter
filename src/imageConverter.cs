using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;

public static class UltimateImageConverter
{
    public static async Task<List<string>> ConvertImagesAsync(
        IEnumerable<string> images, 
        string targetFormat, 
        string outputPath,
        IProgress<double>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        var inputList = images.ToList();
        int totalFiles = inputList.Count;
        if (totalFiles == 0) return [];

        var successfulPaths = new ConcurrentBag<string>();
        var format = ParseFormat(targetFormat);
        int processedFiles = 0;

        // Directory.CreateDirectory safely does nothing if the folder already exists
        Directory.CreateDirectory(outputPath);

        // --- ATOMIC RESERVATION STATE ---
        // Keeps track of filenames being written this exact session to prevent Parallel collisions
        var reservedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pathLock = new object();

        // Limit parallelism to CPU cores to prevent RAM/CPU thrashing on heavy image processing
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Environment.ProcessorCount 
        };

        await Parallel.ForEachAsync(inputList, parallelOptions, async (inputPath, token) =>
        {
            if (!File.Exists(inputPath))
            {
                ReportProgress(ref processedFiles, totalFiles, progress);
                return;
            }

            string finalPath;
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(inputPath);
                string ext = targetFormat.ToLowerInvariant();
                
                // --- BEAST MODE: CONCURRENCY-SAFE FILENAMING ---
                lock (pathLock)
                {
                    finalPath = Path.Combine(outputPath, $"{fileName}.{ext}");
                    int count = 1;
                    
                    // Checks memory reservations FIRST, then disk. Impossible to collide now.
                    while (reservedPaths.Contains(finalPath) || File.Exists(finalPath))
                    {
                        finalPath = Path.Combine(outputPath, $"{fileName} ({count++}).{ext}");
                    }
                    reservedPaths.Add(finalPath);
                }

                // FileShare.Read is mandatory here so multiple threads can read the EXACT same source file
                using var fileStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                using var image = new MagickImage();
                await image.ReadAsync(fileStream, token);

                // --- EXACT METADATA EXTRACTION ---
                double originalDpiX = image.Density?.X ?? 96;
                double originalDpiY = image.Density?.Y ?? 96;
                var originalUnits = image.Density?.Units ?? DensityUnit.PixelsPerInch;

                image.AutoOrient();
                image.Format = format;

                // Re-apply original density and strictly enforce the original units
                image.Density = new Density(originalDpiX, originalDpiY, originalUnits);

                // --- FORMAT-SPECIFIC FLAWLESS PRESERVATION ---
                ApplyFormatSpecificSettings(image, format);

                await image.WriteAsync(finalPath, token);
                successfulPaths.Add(Path.GetFullPath(finalPath));
            }
            catch (Exception ex)
            {
                // Silently logs the exact failure reason without crashing the whole batch
                System.Diagnostics.Debug.WriteLine($"[ERROR] '{inputPath}': {ex.Message}");
            }
            finally
            {
                ReportProgress(ref processedFiles, totalFiles, progress);
            }
        });

        return successfulPaths.ToList();
    }

    private static void ApplyFormatSpecificSettings(MagickImage image, MagickFormat format)
    {
        if (format == MagickFormat.WebP)
        {
            image.Quality = 100;
            image.Settings.SetDefine(MagickFormat.WebP, "lossless", "true"); // Absolute lossless
        }
        else if (format is MagickFormat.Jpeg or MagickFormat.Jpg)
        {
            image.Quality = 100;
            image.Settings.SetDefine(MagickFormat.Jpeg, "sampling-factor", "4:4:4"); // Stops color bleeding
        }
        else if (format == MagickFormat.Ico)
        {
            if (image.Width > 256 || image.Height > 256)
            {
                var size = new MagickGeometry(256, 256) { IgnoreAspectRatio = false };
                image.Resize(size);
            }
        }
    }

    private static void ReportProgress(ref int processedFiles, int totalFiles, IProgress<double>? progress)
    {
        if (progress == null) return;
        int currentProcessed = Interlocked.Increment(ref processedFiles);
        progress.Report((double)currentProcessed / totalFiles * 100);
    }

    private static MagickFormat ParseFormat(string format)
    {
        return format.ToLowerInvariant().TrimStart('.') switch
        {
            "png" => MagickFormat.Png,
            "jpg" or "jpeg" => MagickFormat.Jpeg,
            "bmp" => MagickFormat.Bmp,
            "tiff" or "tif" => MagickFormat.Tiff,
            "webp" => MagickFormat.WebP,
            "ico" => MagickFormat.Ico,
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };
    }
}