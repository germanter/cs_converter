
// // ORIGINAL CODE
// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using ImageMagick;
// using PdfSharp.Drawing;
// using PdfSharp.Pdf;

// namespace ImageToPdfApp;

// #region Interfaces & Options

// public enum PageSizeOption { FitToImage, A4, A3, Square, Letter, Legal }
// public enum OrientationOption { Auto, Portrait, Landscape }
// public enum MarginOption { None = 0, Mm10 = 10, Mm20 = 20, Mm30 = 30 }
// public enum ImageFitOption { FitKeepRatio, StretchToFill, ActualSize }

// /// <summary>Rotation matrix steps. 0 = 0°, 1 = 90°, 2 = 180°, 3 = 270°.</summary>
// public enum RotationSteps { None = 0, Right90 = 1, UpsideDown180 = 2, Left270 = 3 }

// /// <summary>Defines a single ingestion source for the engine.</summary>
// public record ImageInput(string FilePath, RotationSteps Rotation = RotationSteps.None);

// #endregion

// /// <summary>
// /// High-performance multi-threaded image-to-PDF engine. Engineered to out-render browser canvas
// /// implementations using parallel execution, 4:4:4 sampling, 96-DPI scaling, and smart trimming.
// /// </summary>
// public static class ImageToPdfEngine
// {
//     private sealed class RuntimeResourceBag : IDisposable
//     {
//         private readonly List<IDisposable> _items = new();

//         public void Add(IDisposable item)
//         {
//             lock (_items)
//             {
//                 _items.Add(item);
//             }
//         }

//         public void Dispose()
//         {
//             for (int i = _items.Count - 1; i >= 0; i--)
//             {
//                 try { _items[i]?.Dispose(); }
//                 catch { /* swallow cleanup exceptions */ }
//             }
//             _items.Clear();
//         }
//     }

//     /// <summary>
//     /// Temporary transport payload to move optimized image data from parallel CPU cores to the serial PDF writer.
//     /// </summary>
//     private record ProcessedFrameData(
//         bool IsPassthrough,
//         string? FilePath,
//         MemoryStream? DataStream,
//         double PixelWidth,
//         double PixelHeight
//     );

//     /// <summary>
//     /// Locks the virtual canvas (Page) to the exact pixel bounds of the image
//     /// and sets the background color to transparent so rotation does not bleed
//     /// white into alpha channels.
//     /// </summary>
//     private static void NormalizeCanvas(MagickImage image)
//     {
//         image.Page = new MagickGeometry(0, 0, image.Width, image.Height);
//         image.BackgroundColor = MagickColors.Transparent;
//     }

//     /// <summary>
//     /// PdfSharp natively supports JPEG, PNG, BMP, GIF, TIFF.
//     /// We use a robust string check because MagickFormat has variants
//     /// like Png24, Png32, Tiff64, etc., that simple equality misses.
//     /// </summary>
//     private static bool IsNativePdfFormat(MagickFormat format)
//     {
//         var name = format.ToString().ToLowerInvariant();
//         return name.StartsWith("jpeg") || 
//                name.StartsWith("jpg") ||
//                name.StartsWith("png");
//     }

//     public static string ConvertToPdf(
//         IEnumerable<ImageInput> images,
//         string saveDirectory,
//         string filename,
//         PageSizeOption pageSize = PageSizeOption.A4,
//         OrientationOption orientation = OrientationOption.Auto,
//         MarginOption margin = MarginOption.None,
//         ImageFitOption imageFit = ImageFitOption.FitKeepRatio,
//         int quality = 100)
//     {
//         if (images is null || !images.Any())
//             throw new ArgumentException("Image payload cannot be empty.", nameof(images));

//         quality = Math.Clamp(quality, 0, 100);

//         string finalPath = GetSafeFilePath(saveDirectory, filename);

//         using var document = new PdfDocument();
//         document.Info.Title = Path.GetFileNameWithoutExtension(finalPath);
//         document.ViewerPreferences.FitWindow = true;

//         var runtime = new RuntimeResourceBag();

//         try
//         {
//             // ====================================================================================
//             // PHASE 1: The Multi-Threaded Heavy Lifting (Parallel Processing)
//             // ====================================================================================
//             // Images are distributed across all CPU cores. MemoryStreams and pre-calculated
//             // canvas sizes are compiled simultaneously into a flat, order-preserved list.
//             var processedFrames = images
//                 .AsParallel()
//                 .AsOrdered()
//                 .SelectMany(input =>
//                 {
//                     if (string.IsNullOrWhiteSpace(input.FilePath))
//                         throw new ArgumentException("FilePath cannot be empty.", nameof(images));

//                     if (!File.Exists(input.FilePath))
//                         throw new FileNotFoundException($"Input image missing from disk: {input.FilePath}");

//                     var frames = new List<ProcessedFrameData>();

//                     using var rawCollection = new MagickImageCollection(input.FilePath);

//                     var detectedFormat = rawCollection.Count > 0 ? rawCollection[0].Format : MagickFormat.Unknown;
//                     bool isIco = detectedFormat == MagickFormat.Icon || detectedFormat == MagickFormat.Ico;
//                     bool isAnimated = detectedFormat == MagickFormat.Gif ||
//                                       detectedFormat == MagickFormat.WebP ||
//                                       detectedFormat == MagickFormat.Avif;

//                     if (isIco && rawCollection.Count > 1)
//                     {
//                         var bestFrame = (MagickImage)rawCollection
//                             .OrderByDescending(x => x.Width * x.Height)
//                             .First()
//                             .Clone();

//                         // CRITICAL SHORTCUT 2: Memory Fix for Icons
//                         // Instead of RemoveAt(0) which shifts heavy blocks of memory iteratively,
//                         // instantly dispose and wipe the collection clean.
//                         foreach (var img in rawCollection)
//                         {
//                             img.Dispose();
//                         }
//                         rawCollection.Clear();

//                         NormalizeCanvas(bestFrame);
//                         rawCollection.Add(bestFrame);
//                     }
//                     else if (isAnimated)
//                     {
//                         rawCollection.Coalesce();
//                     }

//                     // Explicit cast in foreach because the indexer/enumerator exposes
//                     // IMagickImage<ushort> but the runtime type is always MagickImage.
//                     foreach (MagickImage img in rawCollection)
//                         NormalizeCanvas(img);

//                     var fmt = rawCollection[0].Format;
//                     bool isNativePdfSupported = IsNativePdfFormat(fmt);

//                     // PURE ZERO-LOSS PASSTHROUGH PATH
//                     if (quality == 100 && input.Rotation == RotationSteps.None && rawCollection.Count == 1 && isNativePdfSupported)
//                     {
//                         frames.Add(new ProcessedFrameData(
//                             IsPassthrough: true,
//                             FilePath: input.FilePath,
//                             DataStream: null,
//                             PixelWidth: rawCollection[0].Width,
//                             PixelHeight: rawCollection[0].Height
//                         ));
//                         return frames;
//                     }

//                     // COMPRESSION / MODIFICATION PATH
//                     foreach (MagickImage magickImg in rawCollection)
//                     {
//                         if (input.Rotation != RotationSteps.None)
//                         {
//                             switch (input.Rotation)
//                             {
//                                 case RotationSteps.Right90:
//                                     magickImg.Rotate(90);
//                                     break;
//                                 case RotationSteps.UpsideDown180:
//                                     magickImg.Flip();
//                                     magickImg.Flop();
//                                     break;
//                                 case RotationSteps.Left270:
//                                     magickImg.Rotate(270);
//                                     break;
//                             }
//                         }

//                         // CRITICAL SHORTCUT 1: Smart Trimming
//                         // JPEGs cannot support transparent bounds. Avoid scanning millions of pixels for free.
//                         bool isJpeg = magickImg.Format == MagickFormat.Jpeg || magickImg.Format == MagickFormat.Jpg;
//                         if (!isJpeg)
//                         {
//                             magickImg.Trim();
//                         }

//                         NormalizeCanvas(magickImg);
//                         magickImg.Strip();

//                         var ms = new MemoryStream();

//                         if (quality < 100)
//                         {
//                             magickImg.Settings.SetDefine(MagickFormat.Jpeg, "sampling-factor", "4:4:4");
//                             magickImg.ColorSpace = ColorSpace.sRGB;

//                             if (magickImg.HasAlpha)
//                             {
//                                 using var solidCanvas = new MagickImage(MagickColors.White, magickImg.Width, magickImg.Height);
//                                 solidCanvas.Composite(magickImg, CompositeOperator.Over);
//                                 solidCanvas.Format = MagickFormat.Jpeg;
//                                 solidCanvas.Quality = (uint)quality;
//                                 solidCanvas.Write(ms);
//                             }
//                             else
//                             {
//                                 magickImg.Format = MagickFormat.Jpeg;
//                                 magickImg.Quality = (uint)quality;
//                                 magickImg.Write(ms);
//                             }
//                         }
//                         else
//                         {
//                             if (magickImg.HasAlpha)
//                                 {
//                                     // If the image has transparency, we MUST use PNG.
//                                     // Level 1 stops PdfSharp from choking on giant uncompressed streams.
//                                     magickImg.Format = MagickFormat.Png;
//                                     magickImg.Settings.SetDefine(MagickFormat.Png, "compression-level", "1");
//                                     magickImg.Write(ms);
//                                 }
//                                 else
//                                 {
//                                     // For standard photos/images, use Maximum Quality JPEG (4:4:4 sampling).
//                                     // This completely bypasses PdfSharp's slow compression trap!
//                                     magickImg.Format = MagickFormat.Jpeg;
//                                     magickImg.Quality = 100;
//                                     magickImg.Settings.SetDefine(MagickFormat.Jpeg, "sampling-factor", "4:4:4");
//                                     magickImg.Write(ms);
//                                 }
//                         }

//                         ms.Position = 0;

//                         // CRITICAL SHORTCUT 3: Pre-calculating Canvas Sizes right from MagickImg 
//                         // so PdfSharp doesn't have to evaluate bounds.
//                         frames.Add(new ProcessedFrameData(
//                             IsPassthrough: false,
//                             FilePath: null,
//                             DataStream: ms,
//                             PixelWidth: magickImg.Width,
//                             PixelHeight: magickImg.Height
//                         ));
//                     }

//                     return frames;
//                 })
//                 .ToList();

//             // ====================================================================================
//             // PHASE 2: The Fast Assembly Line (Serial Writing)
//             // ====================================================================================
//             // Since all decoding, compressing, and sizing is done, PdfSharp flies 
//             // through the pre-made chunks and binds them consecutively.
//             foreach (var frame in processedFrames)
//             {
//                 Stream streamToUse;
                
//                 if (frame.IsPassthrough)
//                 {
//                     streamToUse = new FileStream(frame.FilePath!, FileMode.Open, FileAccess.Read, FileShare.Read);
//                 }
//                 else
//                 {
//                     streamToUse = frame.DataStream!;
//                 }
                
//                 // Add to garbage bag to ensure strict disposal after document.Save()
//                 runtime.Add(streamToUse);

//                 var xImage = XImage.FromStream(streamToUse);
//                 xImage.Interpolate = false;
//                 runtime.Add(xImage);

//                 var page = document.AddPage();
                
//                 // Bypass checking sizes back and forth by passing them directly in.
//                 CalculateGeometryAndRender(
//                     page, 
//                     xImage, 
//                     frame.PixelWidth, 
//                     frame.PixelHeight, 
//                     pageSize, 
//                     orientation, 
//                     margin, 
//                     imageFit);
//             }

//             document.Save(finalPath);
//             return finalPath;
//         }
//         finally
//         {
//             runtime.Dispose();
//         }
//     }

//     private static void CalculateGeometryAndRender(
//         PdfPage page,
//         XImage xImage,
//         double preCalcPixelWidth,
//         double preCalcPixelHeight,
//         PageSizeOption pageSize,
//         OrientationOption orientation,
//         MarginOption margin,
//         ImageFitOption imageFit)
//     {
//         // THE 96-DPI FIX:
//         // PDF points are strictly 72 DPI. Browsers render canvas natively at 96 DPI.
//         // By multiplying pixels by 0.75 (72/96), we pack the pixels tighter, resulting
//         // in the exact same crisp physical dimensions the JS script produced.
//         const double PxToPt = 0.75;

//         // Use the fast pre-calculated dimensions directly from Phase 1.
//         double imgWidth = preCalcPixelWidth * PxToPt;
//         double imgHeight = preCalcPixelHeight * PxToPt;

//         double pageWidth = 0, pageHeight = 0;

//         switch (pageSize)
//         {
//             case PageSizeOption.A4:
//                 pageWidth = XUnit.FromMillimeter(210).Point;
//                 pageHeight = XUnit.FromMillimeter(297).Point;
//                 break;
//             case PageSizeOption.A3:
//                 pageWidth = XUnit.FromMillimeter(297).Point;
//                 pageHeight = XUnit.FromMillimeter(420).Point;
//                 break;
//             case PageSizeOption.Letter:
//                 pageWidth = XUnit.FromInch(8.5).Point;
//                 pageHeight = XUnit.FromInch(11).Point;
//                 break;
//             case PageSizeOption.Legal:
//                 pageWidth = XUnit.FromInch(8.5).Point;
//                 pageHeight = XUnit.FromInch(14).Point;
//                 break;
//             case PageSizeOption.Square:
//                 pageWidth = XUnit.FromMillimeter(210).Point;
//                 pageHeight = XUnit.FromMillimeter(210).Point;
//                 break;
//             case PageSizeOption.FitToImage:
//                 pageWidth = imgWidth;
//                 pageHeight = imgHeight;
//                 break;
//         }

//         double marginPts = XUnit.FromMillimeter((double)margin).Point;

//         if (pageSize == PageSizeOption.FitToImage)
//         {
//             pageWidth += marginPts * 2;
//             pageHeight += marginPts * 2;
//         }

//         bool isLandscape = orientation == OrientationOption.Auto
//             ? imgWidth > imgHeight
//             : orientation == OrientationOption.Landscape;

//         if (pageSize != PageSizeOption.FitToImage)
//         {
//             if (isLandscape && pageWidth < pageHeight)
//                 (pageWidth, pageHeight) = (pageHeight, pageWidth);
//             else if (!isLandscape && pageWidth > pageHeight)
//                 (pageWidth, pageHeight) = (pageHeight, pageWidth);
//         }

//         page.Width = XUnit.FromPoint(pageWidth);
//         page.Height = XUnit.FromPoint(pageHeight);

//         double drawX = marginPts;
//         double drawY = marginPts;
//         double drawWidth = pageWidth - (marginPts * 2);
//         double drawHeight = pageHeight - (marginPts * 2);

//         if (drawWidth <= 0 || drawHeight <= 0)
//         {
//             drawX = 0;
//             drawY = 0;
//             drawWidth = pageWidth;
//             drawHeight = pageHeight;
//         }

//         double targetX = drawX, targetY = drawY, targetW = drawWidth, targetH = drawHeight;

//         switch (imageFit)
//         {
//             case ImageFitOption.StretchToFill:
//                 break;

//             case ImageFitOption.FitKeepRatio:
//                 double ratio = Math.Min(drawWidth / imgWidth, drawHeight / imgHeight);
//                 targetW = imgWidth * ratio;
//                 targetH = imgHeight * ratio;
//                 targetX = drawX + (drawWidth - targetW) / 2.0;
//                 targetY = drawY + (drawHeight - targetH) / 2.0;
//                 break;

//             case ImageFitOption.ActualSize:
//                 targetW = imgWidth;
//                 targetH = imgHeight;
//                 targetX = drawX + (drawWidth - targetW) / 2.0;
//                 targetY = drawY + (drawHeight - targetH) / 2.0;
//                 break;
//         }

//         using var gfx = XGraphics.FromPdfPage(page);

//         targetX = Math.Round(targetX, 2);
//         targetY = Math.Round(targetY, 2);
//         targetW = Math.Round(targetW, 2);
//         targetH = Math.Round(targetH, 2);

//         gfx.DrawImage(xImage, targetX, targetY, targetW, targetH);
//     }

//     private static string GetSafeFilePath(string directory, string filename)
//     {
//         if (string.IsNullOrWhiteSpace(directory))
//             directory = Environment.CurrentDirectory;

//         if (!Directory.Exists(directory))
//             Directory.CreateDirectory(directory);

//         string baseName = Path.GetFileNameWithoutExtension(filename);
//         string extension = ".pdf";
//         string fullPath = Path.Combine(directory, baseName + extension);

//         int counter = 1;
//         while (File.Exists(fullPath))
//         {
//             fullPath = Path.Combine(directory, $"{baseName} ({counter}){extension}");
//             counter++;
//         }

//         return fullPath;
//     }
// }

///////////////////////////////////////////////////////////////////
/// ///////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////
// ### FULLY SUPPORTED LIST (Native & Optimized Paths)
// jpg
// jpeg
// png
// gif
// webp
// avif
// ico
// bmp
// tiff
// tif
// tga
// psd

// ### EXPERIMENTAL LIST (Machine-Dependent or Rasterized)
// heic   (Requires system-level codecs or specific Magick builds)
// heif   (Requires system-level codecs or specific Magick builds)
// svg    (Works but gets forced into flat pixels, losing vector qualities)

// ### NOT ALLOWED LIST (Will crash, leak memory, or corrupt output)
// pdf    (Do not feed PDFs into an image engine)
// ai     (Vector format requiring external Ghostscript dependencies)
// eps    (Vector format requiring external Ghostscript dependencies)
// txt
// zip
// exe
// dll
// NEW CODE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ImageMagick;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace ImageToPdfApp;

#region Interfaces & Options

public enum PageSizeOption { FitToImage, A4, A3, Square, Letter, Legal }
public enum OrientationOption { Auto, Portrait, Landscape }
public enum MarginOption { None = 0, Mm10 = 10, Mm20 = 20, Mm30 = 30 }
public enum ImageFitOption { FitKeepRatio, StretchToFill, ActualSize }

/// Rotation matrix steps. 0 = 0°, 1 = 90°, 2 = 180°, 3 = 270°.
public enum RotationSteps { None = 0, Right90 = 1, UpsideDown180 = 2, Left270 = 3 }

/// Defines a single ingestion source for the engine.
public record ImageInput(string FilePath, RotationSteps Rotation = RotationSteps.None);

#endregion

/// <summary>
/// High-performance multi-threaded image-to-PDF engine. Engineered to
/// out-render browser canvas implementations using parallel execution, 4:4:4
/// sampling, 96-DPI scaling, and smart trimming.
/// </summary>
public static class ImageToPdfEngine
{
    private sealed class RuntimeResourceBag : IDisposable
    {
        private readonly List<IDisposable> _items = new();

        public void Add(IDisposable item)
        {
            lock (_items)
            {
                _items.Add(item);
            }
        }

        public void Dispose()
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                try { _items[i]?.Dispose(); }
                catch { /* swallow cleanup exceptions */ }
            }
            _items.Clear();
        }
    }

    /// <summary>
    /// Temporary transport payload to move optimized image data from parallel CPU cores to the serial PDF writer.
    /// </summary>
    private record ProcessedFrameData(
        bool IsPassthrough,
        string? FilePath,
        MemoryStream? DataStream,
        double PixelWidth,
        double PixelHeight
    );

    /// <summary>
    /// Locks the virtual canvas (Page) to the exact pixel bounds of the image
    /// and sets the background color to transparent so rotation does not bleed
    /// white into alpha channels.
    /// </summary>
    private static void NormalizeCanvas(MagickImage image)
    {
        image.Page = new MagickGeometry(0, 0, image.Width, image.Height);
        image.BackgroundColor = MagickColors.Transparent;
    }

    /// <summary>
    /// PdfSharp natively supports JPEG, PNG, BMP, GIF, TIFF.
    /// We use a robust string check because MagickFormat has variants
    /// like Png24, Png32, Tiff64, etc., that simple equality misses.
    /// </summary>
    private static bool IsNativePdfFormat(MagickFormat format)
    {
        var name = format.ToString().ToLowerInvariant();
        return name.StartsWith("jpeg") ||
               name.StartsWith("jpg") ||
               name.StartsWith("png");
    }

    private static void ReportProgress(ref int processedFiles, int totalFiles, IProgress<double>? progress)
    {
        if (progress == null) return;
        int currentProcessed = Interlocked.Increment(ref processedFiles);
        progress.Report((double)currentProcessed / totalFiles * 100);
    }

    public static string ConvertToPdf(
        IEnumerable<ImageInput> images,
        string saveDirectory,
        string filename,
        PageSizeOption pageSize = PageSizeOption.A4,
        OrientationOption orientation = OrientationOption.Auto,
        MarginOption margin = MarginOption.None,
        ImageFitOption imageFit = ImageFitOption.FitKeepRatio,
        int quality = 100,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (images is null)
            throw new ArgumentException("Image payload cannot be null.", nameof(images));

        // Prevent multiple enumeration of the IEnumerable and grab total count
        var imageList = images as IReadOnlyCollection<ImageInput> ?? images.ToList();
        if (imageList.Count == 0)
            throw new ArgumentException("Image payload cannot be empty.", nameof(images));

        quality = Math.Clamp(quality, 0, 100);

        string finalPath = GetSafeFilePath(saveDirectory, filename);
        var runtime = new RuntimeResourceBag();

        try
        {
            using var document = new PdfDocument();
            document.Info.Title = Path.GetFileNameWithoutExtension(finalPath);
            document.ViewerPreferences.FitWindow = true;

            int totalFiles = imageList.Count;
            int processedFiles = 0;

            // ====================================================================================
            // PHASE 1: The Multi-Threaded Heavy Lifting (Parallel Processing)
            // ====================================================================================
            // Images are distributed across all CPU cores. MemoryStreams and pre-calculated
            // canvas sizes are compiled simultaneously into a flat, order-preserved list.
            var processedFrames = imageList
                .AsParallel()
                .AsOrdered()
                .WithCancellation(cancellationToken) // Enforce immediate halting via token
                .SelectMany(input =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrWhiteSpace(input.FilePath))
                        throw new ArgumentException("FilePath cannot be empty.", nameof(images));

                    if (!File.Exists(input.FilePath))
                        throw new FileNotFoundException($"Input image missing from disk: {input.FilePath}");

                    var frames = new List<ProcessedFrameData>();

                    using var rawCollection = new MagickImageCollection(input.FilePath);

                    var detectedFormat = rawCollection.Count > 0 ? rawCollection[0].Format : MagickFormat.Unknown;
                    bool isIco = detectedFormat == MagickFormat.Icon || detectedFormat == MagickFormat.Ico;
                    bool isAnimated = detectedFormat == MagickFormat.Gif ||
                                      detectedFormat == MagickFormat.WebP ||
                                      detectedFormat == MagickFormat.Avif;

                    if (isIco && rawCollection.Count > 1)
                    {
                        var bestFrame = (MagickImage)rawCollection
                            .OrderByDescending(x => x.Width * x.Height)
                            .First()
                            .Clone();

                        // CRITICAL SHORTCUT 2: Memory Fix for Icons
                        foreach (var img in rawCollection)
                        {
                            img.Dispose();
                        }
                        rawCollection.Clear();

                        NormalizeCanvas(bestFrame);
                        rawCollection.Add(bestFrame);
                    }
                    else if (isAnimated)
                    {
                        rawCollection.Coalesce();
                    }

                    foreach (MagickImage img in rawCollection)
                        NormalizeCanvas(img);

                    var fmt = rawCollection[0].Format;
                    bool isNativePdfSupported = IsNativePdfFormat(fmt);

                    // PURE ZERO-LOSS PASSTHROUGH PATH
                    if (quality == 100 && input.Rotation == RotationSteps.None && rawCollection.Count == 1 && isNativePdfSupported)
                    {
                        frames.Add(new ProcessedFrameData(
                            IsPassthrough: true,
                            FilePath: input.FilePath,
                            DataStream: null,
                            PixelWidth: rawCollection[0].Width,
                            PixelHeight: rawCollection[0].Height
                        ));
                        
                        ReportProgress(ref processedFiles, totalFiles, progress);
                        return frames;
                    }

                    // COMPRESSION / MODIFICATION PATH
                    foreach (MagickImage magickImg in rawCollection)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (input.Rotation != RotationSteps.None)
                        {
                            switch (input.Rotation)
                            {
                                case RotationSteps.Right90:
                                    magickImg.Rotate(90);
                                    break;
                                case RotationSteps.UpsideDown180:
                                    magickImg.Flip();
                                    magickImg.Flop();
                                    break;
                                case RotationSteps.Left270:
                                    magickImg.Rotate(270);
                                    break;
                            }
                        }

                        // CRITICAL SHORTCUT 1: Smart Trimming
                        bool isJpeg = magickImg.Format == MagickFormat.Jpeg || magickImg.Format == MagickFormat.Jpg;
                        if (!isJpeg)
                        {
                            magickImg.Trim();
                        }

                        NormalizeCanvas(magickImg);
                        magickImg.Strip();

                        var ms = new MemoryStream();
                        // Add to runtime immediately so if cancelled or failed mid-query, it's strictly disposed!
                        runtime.Add(ms); 

                        if (quality < 100)
                        {
                            magickImg.Settings.SetDefine(MagickFormat.Jpeg, "sampling-factor", "4:4:4");
                            magickImg.ColorSpace = ColorSpace.sRGB;

                            if (magickImg.HasAlpha)
                            {
                                using var solidCanvas = new MagickImage(MagickColors.White, magickImg.Width, magickImg.Height);
                                solidCanvas.Composite(magickImg, CompositeOperator.Over);
                                solidCanvas.Format = MagickFormat.Jpeg;
                                solidCanvas.Quality = (uint)quality;
                                solidCanvas.Write(ms);
                            }
                            else
                            {
                                magickImg.Format = MagickFormat.Jpeg;
                                magickImg.Quality = (uint)quality;
                                magickImg.Write(ms);
                            }
                        }
                        else
                        {
                            if (magickImg.HasAlpha)
                            {
                                magickImg.Format = MagickFormat.Png;
                                magickImg.Settings.SetDefine(MagickFormat.Png, "compression-level", "1");
                                magickImg.Write(ms);
                            }
                            else
                            {
                                magickImg.Format = MagickFormat.Jpeg;
                                magickImg.Quality = 100;
                                magickImg.Settings.SetDefine(MagickFormat.Jpeg, "sampling-factor", "4:4:4");
                                magickImg.Write(ms);
                            }
                        }

                        ms.Position = 0;

                        // CRITICAL SHORTCUT 3: Pre-calculating Canvas Sizes right from MagickImg 
                        frames.Add(new ProcessedFrameData(
                            IsPassthrough: false,
                            FilePath: null,
                            DataStream: ms,
                            PixelWidth: magickImg.Width,
                            PixelHeight: magickImg.Height
                        ));
                    }

                    ReportProgress(ref processedFiles, totalFiles, progress);
                    return frames;
                })
                .ToList();

            // ====================================================================================
            // PHASE 2: The Fast Assembly Line (Serial Writing)
            // ====================================================================================
            foreach (var frame in processedFrames)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Stream streamToUse;

                if (frame.IsPassthrough)
                {
                    streamToUse = new FileStream(frame.FilePath!, FileMode.Open, FileAccess.Read, FileShare.Read);
                    runtime.Add(streamToUse);
                }
                else
                {
                    streamToUse = frame.DataStream!;
                    // Note: Memory streams are already registered with runtime bag in Phase 1
                }

                var xImage = XImage.FromStream(streamToUse);
                xImage.Interpolate = false;
                runtime.Add(xImage);

                var page = document.AddPage();

                CalculateGeometryAndRender(
                    page,
                    xImage,
                    frame.PixelWidth,
                    frame.PixelHeight,
                    pageSize,
                    orientation,
                    margin,
                    imageFit);
            }

            cancellationToken.ThrowIfCancellationRequested();
            document.Save(finalPath);
            
            return finalPath;
        }
        catch
        {
            // PROTOCOL ENFORCED: NUKE ENTIRE THING.
            // If user cancels or ANY single image/file/stream fails, zero compromise.
            // Erase the file if partial data managed to be written to disk.
            if (File.Exists(finalPath))
            {
                try { File.Delete(finalPath); } catch { }
            }

            // Immediately rethink and rethrow exception to the caller. No bypassing errors.
            throw; 
        }
        finally
        {
            // All handles, files, memory streams strictly released immediately
            runtime.Dispose();
        }
    }

    private static void CalculateGeometryAndRender(
        PdfPage page,
        XImage xImage,
        double preCalcPixelWidth,
        double preCalcPixelHeight,
        PageSizeOption pageSize,
        OrientationOption orientation,
        MarginOption margin,
        ImageFitOption imageFit)
    {
        // THE 96-DPI FIX:
        // PDF points are strictly 72 DPI. Browsers render canvas natively at 96 DPI.
        // By multiplying pixels by 0.75 (72/96), we pack the pixels tighter, resulting
        // in the exact same crisp physical dimensions the JS script produced.
        const double PxToPt = 0.75;

        // Use the fast pre-calculated dimensions directly from Phase 1.
        double imgWidth = preCalcPixelWidth * PxToPt;
        double imgHeight = preCalcPixelHeight * PxToPt;

        double pageWidth = 0, pageHeight = 0;

        switch (pageSize)
        {
            case PageSizeOption.A4:
                pageWidth = XUnit.FromMillimeter(210).Point;
                pageHeight = XUnit.FromMillimeter(297).Point;
                break;
            case PageSizeOption.A3:
                pageWidth = XUnit.FromMillimeter(297).Point;
                pageHeight = XUnit.FromMillimeter(420).Point;
                break;
            case PageSizeOption.Letter:
                pageWidth = XUnit.FromInch(8.5).Point;
                pageHeight = XUnit.FromInch(11).Point;
                break;
            case PageSizeOption.Legal:
                pageWidth = XUnit.FromInch(8.5).Point;
                pageHeight = XUnit.FromInch(14).Point;
                break;
            case PageSizeOption.Square:
                pageWidth = XUnit.FromMillimeter(210).Point;
                pageHeight = XUnit.FromMillimeter(210).Point;
                break;
            case PageSizeOption.FitToImage:
                pageWidth = imgWidth;
                pageHeight = imgHeight;
                break;
        }

        double marginPts = XUnit.FromMillimeter((double)margin).Point;

        if (pageSize == PageSizeOption.FitToImage)
        {
            pageWidth += marginPts * 2;
            pageHeight += marginPts * 2;
        }

        bool isLandscape = orientation == OrientationOption.Auto
            ? imgWidth > imgHeight
            : orientation == OrientationOption.Landscape;

        if (pageSize != PageSizeOption.FitToImage)
        {
            if (isLandscape && pageWidth < pageHeight)
                (pageWidth, pageHeight) = (pageHeight, pageWidth);
            else if (!isLandscape && pageWidth > pageHeight)
                (pageWidth, pageHeight) = (pageHeight, pageWidth);
        }

        page.Width = XUnit.FromPoint(pageWidth);
        page.Height = XUnit.FromPoint(pageHeight);

        double drawX = marginPts;
        double drawY = marginPts;
        double drawWidth = pageWidth - (marginPts * 2);
        double drawHeight = pageHeight - (marginPts * 2);

        if (drawWidth <= 0 || drawHeight <= 0)
        {
            drawX = 0;
            drawY = 0;
            drawWidth = pageWidth;
            drawHeight = pageHeight;
        }

        double targetX = drawX, targetY = drawY, targetW = drawWidth, targetH = drawHeight;

        switch (imageFit)
        {
            case ImageFitOption.StretchToFill:
                break;

            case ImageFitOption.FitKeepRatio:
                double ratio = Math.Min(drawWidth / imgWidth, drawHeight / imgHeight);
                targetW = imgWidth * ratio;
                targetH = imgHeight * ratio;
                targetX = drawX + (drawWidth - targetW) / 2.0;
                targetY = drawY + (drawHeight - targetH) / 2.0;
                break;

            case ImageFitOption.ActualSize:
                targetW = imgWidth;
                targetH = imgHeight;
                targetX = drawX + (drawWidth - targetW) / 2.0;
                targetY = drawY + (drawHeight - targetH) / 2.0;
                break;
        }

        using var gfx = XGraphics.FromPdfPage(page);

        targetX = Math.Round(targetX, 2);
        targetY = Math.Round(targetY, 2);
        targetW = Math.Round(targetW, 2);
        targetH = Math.Round(targetH, 2);

        gfx.DrawImage(xImage, targetX, targetY, targetW, targetH);
    }

    private static string GetSafeFilePath(string directory, string filename)
    {
        if (string.IsNullOrWhiteSpace(directory))
            directory = Environment.CurrentDirectory;

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string baseName = Path.GetFileNameWithoutExtension(filename);
        string extension = ".pdf";
        string fullPath = Path.Combine(directory, baseName + extension);

        int counter = 1;
        while (File.Exists(fullPath))
        {
            fullPath = Path.Combine(directory, $"{baseName} ({counter}){extension}");
            counter++;
        }

        return fullPath;
    }
}