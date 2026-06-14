
// NEW
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

        Directory.CreateDirectory(outputPath);

        var reservedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pathLock = new object();

        // --- THE ERROR TRAP ---
        Exception? criticalError = null;

        // --- 0 COMPROMISE LINKED TOKEN ---
        using var nukeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var parallelOptions = new ParallelOptions
        {
            CancellationToken = nukeCts.Token,
            MaxDegreeOfParallelism = Environment.ProcessorCount 
        };

        try
        {
            await Parallel.ForEachAsync(inputList, parallelOptions, async (inputPath, token) =>
            {
                token.ThrowIfCancellationRequested();

                var fileInfo = new FileInfo(inputPath);
                if (!fileInfo.Exists) throw new FileNotFoundException($"Input file missing: {inputPath}");
                if (fileInfo.Length == 0) throw new InvalidDataException($"FILE IS EMPTY (0 BYTES): '{inputPath}'.");

                string finalPath = string.Empty;
                MagickImage? image = null;

                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(inputPath);
                    string ext = targetFormat.ToLowerInvariant();
                    
                    lock (pathLock)
                    {
                        finalPath = Path.Combine(outputPath, $"{fileName}.{ext}");
                        int count = 1;
                        
                        while (reservedPaths.Contains(finalPath) || File.Exists(finalPath))
                        {
                            finalPath = Path.Combine(outputPath, $"{fileName} ({count++}).{ext}");
                        }
                        reservedPaths.Add(finalPath);
                    }

                    token.ThrowIfCancellationRequested();

                    // Load entire file into RAM to completely detach from disk I/O stream bugs
                    byte[] fileBytes = await File.ReadAllBytesAsync(inputPath, token);

                    try
                    {
                        // ATTEMPT 1: Strict Auto-Detect (Standard)
                        if (inputPath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                        {
                            using var collection = new MagickImageCollection();
                            collection.Read(fileBytes);
                            
                            if (collection.Count == 0) 
                                throw new InvalidDataException("ICO file contains no frames.");

                            var bestFrame = collection.OrderByDescending(x => x.Width * x.Height).First();
                            image = new MagickImage(bestFrame);
                        }
                        else
                        {
                            image = new MagickImage(fileBytes);
                        }
                    }
                    catch (Exception ex) when (ex is MagickException || ex is InvalidDataException)
                    {
                        // --- ATTEMPT 2: THE BRUTE FORCE DECODER ---
                        // The engine rejected the file. It is likely a renamed PNG/WebP, or the headers are dirty.
                        // We will forcefully bypass the auto-detector and jam the bytes into every decoder.
                        bool recovered = false;
                        MagickFormat[] forceFormats = { 
                            MagickFormat.Png, MagickFormat.WebP, MagickFormat.Jpeg, 
                            MagickFormat.Bmp, MagickFormat.Ico, MagickFormat.Tiff 
                        };

                        foreach (var forceFormat in forceFormats)
                        {
                            try
                            {
                                var settings = new MagickReadSettings { Format = forceFormat };
                                image = new MagickImage(fileBytes, settings); // Bypasses magic bytes check
                                recovered = true;
                                break; // IT WORKED!
                            }
                            catch
                            {
                                image?.Dispose();
                                image = null;
                            }
                        }

                        if (!recovered)
                        {
                            // If it still fails, it's either brutally destroyed, or an OS-specific format (like HEIC/AVIF)
                            throw new InvalidDataException(
                                $"Format Engine Failure: The file '{Path.GetFileName(inputPath)}' is deeply malformed or uses an unsupported codec (like AVIF/HEIC). Even brute-force decoding failed. Original Error: {ex.Message}", ex);
                        }
                    }

                    // --- COMPILER FIX & FINAL SAFETY NET ---
                    // Eliminates "Dereference of a possibly null reference" warning completely.
                    if (image == null)
                    {
                        throw new InvalidOperationException($"CRITICAL: Image processing failed to initialize an object in memory for '{inputPath}'.");
                    }

                    double originalDpiX = image.Density?.X ?? 96;
                    double originalDpiY = image.Density?.Y ?? 96;
                    var originalUnits = image.Density?.Units ?? DensityUnit.PixelsPerInch;

                    image.AutoOrient();
                    image.Format = format;

                    image.Density = new Density(originalDpiX, originalDpiY, originalUnits);

                    ApplyFormatSpecificSettings(image, format);

                    await image.WriteAsync(finalPath, token);
                    successfulPaths.Add(Path.GetFullPath(finalPath));
                }
                catch (OperationCanceledException)
                {
                    throw; 
                }
                catch (Exception ex)
                {
                    // --- TRIGGER THE NUKE & TRAP THE ERROR ---
                    Interlocked.CompareExchange(ref criticalError, ex, null);
                    System.Diagnostics.Debug.WriteLine($"[CRITICAL ABORT] '{inputPath}': {ex.Message}");
                    nukeCts.Cancel(); 
                    throw; 
                }
                finally
                {
                    image?.Dispose();
                    ReportProgress(ref processedFiles, totalFiles, progress);
                }
            });
        }
        catch (Exception) 
        {
            // --- THE NUKE WIPE ---
            foreach (var path in reservedPaths)
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { } 
            }

            if (criticalError != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(criticalError).Throw();
            }

            throw;
        }

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