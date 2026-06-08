
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Docnet.Core;
using Docnet.Core.Models;
using ImageMagick;

namespace PdfEngine
{
    public static class PdfToImageConverter
    {
        private static readonly object PdfLock = new object();

        public static List<string> ConvertPdfToImages(string pdfPath, string outputPath, int dpi = 200, int quality = 90)
        {
            if (!File.Exists(pdfPath))
                throw new FileNotFoundException($"PDF not found: {pdfPath}");

            string safeOutputDir = outputPath;
            if (Directory.Exists(safeOutputDir) && Directory.EnumerateFileSystemEntries(safeOutputDir).Any())
            {
                string uniqueSuffix = "PDF_Export_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                safeOutputDir = Path.Combine(safeOutputDir, uniqueSuffix);
            }
            Directory.CreateDirectory(safeOutputDir);

            var savedFilePaths = new ConcurrentBag<string>();

            // Restrict threads slightly to prevent Large Object Heap (LOH) memory crashes on big PDFs
            int maxThreads = Math.Max(1, Environment.ProcessorCount / 4);
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = maxThreads };

            // STRICTLY use requested DPI. 
            // (Scaling up here is what caused "failed to create a bitmap object" in PDFium)
            double scale = dpi / 72.0;

            using (var docReader = DocLib.Instance.GetDocReader(pdfPath, new PageDimensions(scale)))
            {
                int pageCount = docReader.GetPageCount();
                int padLength = pageCount.ToString().Length;

                Parallel.For(0, pageCount, parallelOptions, i =>
                {
                    byte[] rawBgra;
                    int width, height;

                    lock (PdfLock)
                    {
                        using var pageReader = docReader.GetPageReader(i);
                        width = pageReader.GetPageWidth();
                        height = pageReader.GetPageHeight();
                        rawBgra = pageReader.GetImage(); 
                    }

                    uint uWidth = (uint)width;
                    uint uHeight = (uint)height;

                    int expectedLength = width * height * 4;
                    if (rawBgra.Length < expectedLength)
                    {
                        Array.Resize(ref rawBgra, expectedLength);
                    }

                    var pixelSettings = new PixelReadSettings(uWidth, uHeight, StorageType.Char, "BGRA");

                    using var pageImage = new MagickImage();
                    pageImage.ReadPixels(rawBgra, pixelSettings);

                    // 1. INSTANT BACKGROUND FLATTENING
                    pageImage.HasAlpha = true;
                    pageImage.BackgroundColor = MagickColors.White;
                    pageImage.Alpha(AlphaOption.Remove);

                    // 2. THE "EXTERNAL API" TRICK (NO MEMORY INFLATION)
                    // Leveling mathematically removes the fuzzy grey anti-aliasing around text by 
                    // pushing dark-greys to pure black and light-greys to pure white.
                    pageImage.Level(new Percentage(10), new Percentage(90));

                    // 3. MICRO-CONTRAST (UNSHARP MASK)
                    // Makes the edges punchy and perfectly mimics high-tier vector rasterization.
                    pageImage.UnsharpMask(radius: 0, sigma: 1.0, amount: 1.0, threshold: 0.01);

                    // 4. FAST, HIGH-QUALITY COMPRESSION
                    pageImage.Format = MagickFormat.Jpeg;
                    pageImage.Quality = (uint)quality; // Dynamic parameter mapping
                    pageImage.Settings.SetDefine(MagickFormat.Jpeg, "sampling-factor", "4:4:4");

                    string paddedIndex = (i + 1).ToString().PadLeft(padLength, '0');

                    // SAVE AS .JPG
                    string fileName = Path.Combine(safeOutputDir, $"page_{paddedIndex}.jpg");
                    pageImage.Write(fileName);

                    savedFilePaths.Add(fileName);
                });
            }

            return savedFilePaths.OrderBy(p => p).ToList();
        }
    }
}