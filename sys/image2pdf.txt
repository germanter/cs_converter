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

// /// <summary>
// /// Rotation matrix steps. 0 = 0°, 1 = 90°, 2 = 180°, 3 = 270°.
// /// </summary>
// public enum RotationSteps { None = 0, Right90 = 1, UpsideDown180 = 2, Left270 = 3 }

// /// <summary>
// /// Defines a single ingestion source for the engine, encapsulating path and state modifiers.
// /// </summary>
// public record ImageInput(string FilePath, RotationSteps Rotation = RotationSteps.None);

// #endregion

// /// <summary>
// /// High-performance image-to-PDF engine with absolute zero-loss visual and data passthrough for HD images.
// /// </summary>
// public static class ImageToPdfEngine
// {
//     private sealed class RuntimeResourceBag : IDisposable
//     {
//         private readonly List<IDisposable> _items = new();

//         public void Add(IDisposable item) => _items.Add(item);

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

//         var runtime = new RuntimeResourceBag();

//         try
//         {
//             foreach (var input in images)
//             {
//                 if (string.IsNullOrWhiteSpace(input.FilePath))
//                     throw new ArgumentException("FilePath cannot be empty.", nameof(images));

//                 if (!File.Exists(input.FilePath))
//                     throw new FileNotFoundException($"Input image missing from disk: {input.FilePath}");

//                 using var rawCollection = new MagickImageCollection(input.FilePath);
//                 rawCollection.Coalesce();

//                 // Absolute-best path:
//                 // quality == 100, no rotation, single-frame => use original file bytes directly.
//                 if (quality == 100 &&
//                     input.Rotation == RotationSteps.None &&
//                     rawCollection.Count == 1)
//                 {
//                     var fileStream = new FileStream(
//                         input.FilePath,
//                         FileMode.Open,
//                         FileAccess.Read,
//                         FileShare.Read);

//                     runtime.Add(fileStream);

//                     var xImage = XImage.FromStream(fileStream);
                    
//                     // CRITICAL FIX: Disables PDF Viewer Anti-Aliasing (Smoothing/Blur)
//                     xImage.Interpolate = false; 
                    
//                     runtime.Add(xImage);

//                     var page = document.AddPage();
//                     CalculateGeometryAndRender(page, xImage, pageSize, orientation, margin, imageFit);
//                     continue;
//                 }

//                 foreach (var magickImg in rawCollection)
//                 {
//                     if (input.Rotation != RotationSteps.None)
//                     {
//                         magickImg.Rotate((int)input.Rotation * 90);
//                     }

//                     var ms = new MemoryStream();
//                     runtime.Add(ms);

//                     if (quality < 100)
//                     {
//                         if (magickImg.HasAlpha)
//                         {
//                             using var solidCanvas = new MagickImage(MagickColors.White, magickImg.Width, magickImg.Height);
//                             solidCanvas.Composite(magickImg, CompositeOperator.Over);
//                             solidCanvas.Format = MagickFormat.Jpeg;
//                             solidCanvas.Quality = (uint)quality;
//                             solidCanvas.Write(ms);
//                         }
//                         else
//                         {
//                             magickImg.Format = MagickFormat.Jpeg;
//                             magickImg.Quality = (uint)quality;
//                             magickImg.Write(ms);
//                         }
//                     }
//                     else
//                     {
//                         // 100-quality path: PNG is mathematically lossless
//                         magickImg.Format = MagickFormat.Png;
//                         magickImg.Write(ms);
//                     }

//                     ms.Position = 0;

//                     var xImage = XImage.FromStream(ms);
                    
//                     // CRITICAL FIX: Ensure rotated or multi-frame lossless images also don't blur
//                     xImage.Interpolate = false; 
                    
//                     runtime.Add(xImage);

//                     var page = document.AddPage();
//                     CalculateGeometryAndRender(page, xImage, pageSize, orientation, margin, imageFit);
//                 }
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
//         PageSizeOption pageSize,
//         OrientationOption orientation,
//         MarginOption margin,
//         ImageFitOption imageFit)
//     {
//         // For digital HD crispness, we anchor dimensions directly to native pixels, 
//         // bypassing arbitrary DPI scaling tags that might exist in the PNG headers.
//         double imgWidth = xImage.PixelWidth; 
//         double imgHeight = xImage.PixelHeight;

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

//         // Sub-pixel rounding avoids the tiny fractional coordinates which can trigger 
//         // very slight blur artifacts on the boundaries inside certain PDF readers.
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











using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageMagick;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace ImageToPdfApp;

#region Interfaces & Options

public enum PageSizeOption { FitToImage, A4, A3, Square, Letter, Legal }
public enum OrientationOption { Auto, Portrait, Landscape }
public enum MarginOption { None = 0, Mm10 = 10, Mm20 = 20, Mm30 = 30 }
public enum ImageFitOption { FitKeepRatio, StretchToFill, ActualSize }

/// <summary>Rotation matrix steps. 0 = 0°, 1 = 90°, 2 = 180°, 3 = 270°.</summary>
public enum RotationSteps { None = 0, Right90 = 1, UpsideDown180 = 2, Left270 = 3 }

/// <summary>Defines a single ingestion source for the engine.</summary>
public record ImageInput(string FilePath, RotationSteps Rotation = RotationSteps.None);

#endregion

/// <summary>
/// High-performance image-to-PDF engine. Engineered to out-render browser canvas
/// implementations using 4:4:4 sampling, 96-DPI scaling, and metadata stripping.
/// </summary>
public static class ImageToPdfEngine
{
    private sealed class RuntimeResourceBag : IDisposable
    {
        private readonly List<IDisposable> _items = new();

        public void Add(IDisposable item) => _items.Add(item);

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
        return name.StartsWith("jpeg") || name.StartsWith("jpg") ||
               name.StartsWith("png") ||
               name == "bmp" ||
               name == "gif" ||
               name.StartsWith("tiff") || name.StartsWith("tif");
    }

    public static string ConvertToPdf(
        IEnumerable<ImageInput> images,
        string saveDirectory,
        string filename,
        PageSizeOption pageSize = PageSizeOption.A4,
        OrientationOption orientation = OrientationOption.Auto,
        MarginOption margin = MarginOption.None,
        ImageFitOption imageFit = ImageFitOption.FitKeepRatio,
        int quality = 100)
    {
        if (images is null || !images.Any())
            throw new ArgumentException("Image payload cannot be empty.", nameof(images));

        quality = Math.Clamp(quality, 0, 100);

        string finalPath = GetSafeFilePath(saveDirectory, filename);

        using var document = new PdfDocument();
        document.Info.Title = Path.GetFileNameWithoutExtension(finalPath);
        document.ViewerPreferences.FitWindow = true;

        var runtime = new RuntimeResourceBag();

        try
        {
            foreach (var input in images)
            {
                if (string.IsNullOrWhiteSpace(input.FilePath))
                    throw new ArgumentException("FilePath cannot be empty.", nameof(images));

                if (!File.Exists(input.FilePath))
                    throw new FileNotFoundException($"Input image missing from disk: {input.FilePath}");

                using var rawCollection = new MagickImageCollection(input.FilePath);

                // Detect format BEFORE coalescing. Coalesce() is for animation frames.
                // ICO stores multiple resolutions — coalescing overlays them and corrupts the image.
                var detectedFormat = rawCollection.Count > 0 ? rawCollection[0].Format : MagickFormat.Unknown;
                bool isIco = detectedFormat == MagickFormat.Icon || detectedFormat == MagickFormat.Ico;
                bool isAnimated = detectedFormat == MagickFormat.Gif ||
                                  detectedFormat == MagickFormat.WebP ||
                                  detectedFormat == MagickFormat.Avif;

                if (isIco && rawCollection.Count > 1)
                {
                    // OrderByDescending on the collection returns IMagickImage<<ushort>.
                    // Clone() also returns the interface. We cast to MagickImage because
                    // MagickImageCollection.Add() expects the concrete type.
                    var bestFrame = (MagickImage)rawCollection
                        .OrderByDescending(x => x.Width * x.Height)
                        .First()
                        .Clone();

                    // Purge the collection so the corrupted frames are not processed.
                    while (rawCollection.Count > 0)
                    {
                        var img = rawCollection[0];
                        rawCollection.RemoveAt(0);
                        img.Dispose();
                    }

                    NormalizeCanvas(bestFrame);
                    rawCollection.Add(bestFrame);
                }
                else if (isAnimated)
                {
                    // GIF / animated WebP / animated AVIF: these are actual animation frames.
                    rawCollection.Coalesce();
                }

                // For every remaining image, lock the virtual canvas to pixel bounds.
                // Explicit cast in foreach because the indexer/enumerator exposes
                // IMagickImage<<ushort> but the runtime type is always MagickImage.
                foreach (MagickImage img in rawCollection)
                    NormalizeCanvas(img);

                var fmt = rawCollection[0].Format;
                bool isNativePdfSupported = IsNativePdfFormat(fmt);

                // PURE ZERO-LOSS PASSTHROUGH PATH — only for non-rotated native images.
                if (quality == 100 && input.Rotation == RotationSteps.None && rawCollection.Count == 1 && isNativePdfSupported)
                {
                    var fileStream = new FileStream(input.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    runtime.Add(fileStream);

                    var xImage = XImage.FromStream(fileStream);
                    xImage.Interpolate = false;
                    runtime.Add(xImage);

                    var page = document.AddPage();
                    CalculateGeometryAndRender(page, xImage, pageSize, orientation, margin, imageFit);
                    continue;
                }

                // COMPRESSION / MODIFICATION PATH
                foreach (MagickImage magickImg in rawCollection)
                {
                    // CRITICAL FIX 1: 180° rotation uses Flip+Flop — pure pixel rearrangement.
                    // No new canvas, no re-compositing, no white matte bleed into alpha.
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

                    // CRITICAL FIX 2: Trim removes transparent/background-colored edges.
                    // ICO files have transparent padding. Rotate() can leave a transparent
                    // fringe on the new canvas. Trim() physically crops to the bounding
                    // box of non-background pixels, killing the white holders.
                    magickImg.Trim();

                    // CRITICAL FIX 3: Trim() changes dimensions. Re-lock the canvas so
                    // PdfSharp reads the exact trimmed pixel bounds with zero virtual padding.
                    NormalizeCanvas(magickImg);

                    // CRITICAL FIX 4: Strip metadata in ALL modification paths, even PNG.
                    // EXIF orientation, density hints, and page metadata must not influence
                    // PdfSharp's interpretation of the image bounds.
                    magickImg.Strip();

                    var ms = new MemoryStream();
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
                        // PNG path: alpha is preserved, no white background introduced.
                        magickImg.Format = MagickFormat.Png;
                        magickImg.Write(ms);
                    }

                    ms.Position = 0;

                    var xImage = XImage.FromStream(ms);
                    xImage.Interpolate = false;
                    runtime.Add(xImage);

                    var page = document.AddPage();
                    CalculateGeometryAndRender(page, xImage, pageSize, orientation, margin, imageFit);
                }
            }

            document.Save(finalPath);
            return finalPath;
        }
        finally
        {
            runtime.Dispose();
        }
    }

    private static void CalculateGeometryAndRender(
        PdfPage page,
        XImage xImage,
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

        double imgWidth = xImage.PixelWidth * PxToPt;
        double imgHeight = xImage.PixelHeight * PxToPt;

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



