// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;

// // Namespaces inferred from your operational core engines
// using ImageToPdfApp;
// using PdfEngine;
// using PdfUtilities;
// using Orchestration;
// // UltimateImageConverter is assumed to be accessible in the current context.

// namespace CentralGateway
// {
//     public static class CentralController
//     {
//         /// <summary>
//         /// DRY Helper to validate file extensions rigidly.
//         /// </summary>
//         private static bool AreExtensionsValid(IEnumerable<string> filePaths, string[] allowedExtensions)
//         {
//             foreach (var path in filePaths)
//             {
//                 if (string.IsNullOrWhiteSpace(path)) 
//                     return false;

//                 string ext = Path.GetExtension(path).ToLowerInvariant();
//                 if (!allowedExtensions.Contains(ext)) 
//                     return false;
//             }
//             return true;
//         }

//         public static string Image2PdfCaller(
//             List<ImageInput> images,
//             string saveDirectory,
//             string filename,
//             PageSizeOption pageSize,
//             OrientationOption orientation,
//             MarginOption margin,
//             ImageFitOption imageFit,
//             int quality,
//             IProgress<double>? progress = null,
//             CancellationToken cancellationToken = default)
//         {
//             // --- CONFIGURABLE VARIABLES ---
//             string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".ico", ".bmp", ".tiff", ".tif", ".tga", ".psd" };
//             string errUnsupportedType = "unsupported image type";
//             string errEngine = "internal engine error";
//             // ------------------------------

//             if (!AreExtensionsValid(images.Select(img => img.FilePath), allowedExtensions))
//             {
//                 throw new Exception(errUnsupportedType);
//             }

//             try
//             {
//                 return ImageToPdfEngine.ConvertToPdf(
//                     images: images,
//                     saveDirectory: saveDirectory,
//                     filename: filename,
//                     pageSize: pageSize,
//                     orientation: orientation,
//                     margin: margin,
//                     imageFit: imageFit,
//                     quality: quality,
//                     progress: progress,
//                     cancellationToken: cancellationToken
//                 );
//             }
//             catch (OperationCanceledException)
//             {
//                 throw; // Bubble up intentional user cancellations unchanged
//             }
//             catch (Exception)
//             {
//                 throw new Exception(errEngine);
//             }
//         }

//         public static async Task<List<string>> ImageConverterCaller(
//             string[] sourceImages,
//             string targetFormat,
//             string outputPath,
//             IProgress<double>? progress = null,
//             CancellationToken cancellationToken = default)
//         {
//             // --- CONFIGURABLE VARIABLES ---
//             string[] allowedExtensions = { ".jpg", ".jpeg", ".ico", ".png", ".webp", ".bmp", ".tiff", ".tif" };
//             string errUnsupportedType = "unsupported image type";
//             string errEngine = "internal engine error";
//             // ------------------------------

//             if (!AreExtensionsValid(sourceImages, allowedExtensions))
//             {
//                 throw new Exception(errUnsupportedType);
//             }

//             try
//             {
//                 // Task is awaited to safely catch internal engine errors inside the try-catch block
//                 var result = await UltimateImageConverter.ConvertImagesAsync(
//                     sourceImages,
//                     targetFormat,
//                     outputPath,
//                     progress,
//                     cancellationToken
//                 );

//                 return result;
//             }
//             catch (OperationCanceledException)
//             {
//                 throw;
//             }
//             catch (Exception)
//             {
//                 throw new Exception(errEngine);
//             }
//         }

//         public static string[] Office2PdfCaller(
//             string[] inputPaths,
//             string newFileName,
//             string filePathToSave,
//             string libreOfficeExePath,
//             string mode,
//             int merge,
//             IProgress<double>? progress = null,
//             CancellationToken cancellationToken = default)
//         {
//             // --- CONFIGURABLE VARIABLES ---
//             string errHomogeneity = "homogenity error";
//             string errEngine = "internal engine error";
//             // ------------------------------

//             string expectedExtension = mode == "docx-pdf" ? ".docx" : 
//                                        mode == "pptx-pdf" ? ".pptx" : 
//                                        string.Empty;

//             if (string.IsNullOrEmpty(expectedExtension))
//             {
//                 throw new Exception(errHomogeneity);
//             }

//             // Strict Homogeneity Enforcer
//             bool isHomogeneous = inputPaths.All(path => 
//                 !string.IsNullOrWhiteSpace(path) && 
//                 Path.GetExtension(path).Equals(expectedExtension, StringComparison.OrdinalIgnoreCase));

//             if (!isHomogeneous)
//             {
//                 throw new Exception(errHomogeneity);
//             }

//             try
//             {
//                 return OfficeBatchToPdfMerger.ConvertAndMerge(
//                     inputPaths: inputPaths,
//                     newFileName: newFileName,
//                     filePathToSave: filePathToSave,
//                     libreOfficeExePath: libreOfficeExePath,
//                     mode: mode,
//                     merge: merge,
//                     progress: progress,
//                     cancellationToken: cancellationToken
//                 );
//             }
//             catch (OperationCanceledException)
//             {
//                 throw;
//             }
//             catch (Exception)
//             {
//                 throw new Exception(errEngine);
//             }
//         }

//         public static List<string> Pdf2ImageCaller(
//             string pdfPath,
//             string outputPath,
//             int dpi,
//             int quality,
//             IProgress<double>? progress = null,
//             CancellationToken cancellationToken = default)
//         {
//             // --- CONFIGURABLE VARIABLES ---
//             string[] allowedExtensions = { ".pdf" };
//             string errNoPdf = "no pdf error";
//             string errEngine = "internal engine error";
//             // ------------------------------

//             if (!AreExtensionsValid(new[] { pdfPath }, allowedExtensions))
//             {
//                 throw new Exception(errNoPdf);
//             }

//             try
//             {
//                 return PdfToImageConverter.ConvertPdfToImages(
//                     pdfPath: pdfPath,
//                     outputPath: outputPath,
//                     dpi: dpi,
//                     quality: quality,
//                     progress: progress,
//                     cancellationToken: cancellationToken
//                 );
//             }
//             catch (OperationCanceledException)
//             {
//                 throw;
//             }
//             catch (Exception)
//             {
//                 throw new Exception(errEngine);
//             }
//         }

//         public static string PdfMergerCaller(
//             string[] pdfPaths,
//             string filePathToSave,
//             string newFileName,
//             IProgress<double>? progress = null,
//             CancellationToken cancellationToken = default)
//         {
//             // --- CONFIGURABLE VARIABLES ---
//             string[] allowedExtensions = { ".pdf" };
//             string errNoPdf = "no pdf error";
//             string errEngine = "internal engine error";
//             // ------------------------------

//             if (!AreExtensionsValid(pdfPaths, allowedExtensions))
//             {
//                 throw new Exception(errNoPdf);
//             }

//             try
//             {
//                 return PdfMerger.Merge(
//                     pdfPaths: pdfPaths,
//                     filePathToSave: filePathToSave,
//                     newFileName: newFileName,
//                     progress: progress,
//                     cancellationToken: cancellationToken
//                 );
//             }
//             catch (OperationCanceledException)
//             {
//                 throw;
//             }
//             catch (Exception)
//             {
//                 throw new Exception(errEngine);
//             }
//         }
//     }
// }























// new code
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Namespaces inferred from your operational core engines
using ImageToPdfApp;
using PdfEngine;
using PdfUtilities;
using Orchestration;
using Glo;


namespace CentralGateway
{
    public static class CentralController
    {
        /// <summary>
        /// DRY Helper to validate file extensions rigidly.
        /// </summary>
        
        // Global static variable accessible from any script in the application
        private static bool AreExtensionsValid(IEnumerable<string> filePaths, string[] allowedExtensions)
        {
            foreach (var path in filePaths)
            {
                if (string.IsNullOrWhiteSpace(path)) 
                    return false;

                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext)) 
                    return false;
            }
            return true;
        }

        public static async Task<string> Image2PdfCallerAsync(
            List<ImageInput> images,
            string saveDirectory,
            string filename,
            PageSizeOption pageSize,
            OrientationOption orientation,
            MarginOption margin,
            ImageFitOption imageFit,
            int quality,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            // --- CONFIGURABLE VARIABLES ---
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".ico", ".bmp", ".tiff", ".tif", ".tga", ".psd" };
            string errUnsupportedType = "unsupported image type";
            string errEngine = "internal engine error";
            // ------------------------------

            // Lightweight validation runs instantly on the calling UI thread
            if (!AreExtensionsValid(images.Select(img => img.FilePath), allowedExtensions))
            {
                throw new Exception(errUnsupportedType);
            }

            try
            {
                // UI Thread Yields Instantly: Handoff heavy processing to the thread-pool
                return await Task.Run(() => ImageToPdfEngine.ConvertToPdf(
                    images: images,
                    saveDirectory: saveDirectory,
                    filename: filename,
                    pageSize: pageSize,
                    orientation: orientation,
                    margin: margin,
                    imageFit: imageFit,
                    quality: quality,
                    progress: progress,
                    cancellationToken: cancellationToken
                ), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw; // Bubble up intentional user cancellations unchanged natively
            }
            catch (Exception)
            {
                throw new Exception(errEngine);
            }
        }

        public static async Task<List<string>> ImageConverterCallerAsync(
            string[] sourceImages,
            string targetFormat,
            string outputPath,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            // --- CONFIGURABLE VARIABLES ---
            string[] allowedExtensions = { ".jpg", ".jpeg", ".ico", ".png", ".webp", ".bmp", ".tiff", ".tif" };
            string errUnsupportedType = "unsupported image type";
            string errEngine = "internal engine error";
            // ------------------------------

            // Lightweight validation runs instantly
            if (!AreExtensionsValid(sourceImages, allowedExtensions))
            {
                throw new Exception(errUnsupportedType);
            }

            try
            {
                // UltimateImageConverter natively handles async logic, so we just await it directly.
                // The UI yields smoothly here.
                var result = await UltimateImageConverter.ConvertImagesAsync(
                    sourceImages,
                    targetFormat,
                    outputPath,
                    progress,
                    cancellationToken
                );

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new Exception(errEngine);
            }
        }

        public static async Task<string[]> Office2PdfCallerAsync(
            string[] inputPaths,
            string newFileName,
            string filePathToSave,
            string mode,
            int merge,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            // --- CONFIGURABLE VARIABLES ---
            string errHomogeneity = "homogenity error";
            string errEngine = "internal engine error";
            // ------------------------------

            string expectedExtension = mode == "docx-pdf" ? ".docx" : 
                                       mode == "pptx-pdf" ? ".pptx" : 
                                       string.Empty;

            if (string.IsNullOrEmpty(expectedExtension))
            {
                throw new Exception(errHomogeneity);
            }

            // Strict Homogeneity Enforcer - runs efficiently on the UI thread
            bool isHomogeneous = inputPaths.All(path => 
                !string.IsNullOrWhiteSpace(path) && 
                Path.GetExtension(path).Equals(expectedExtension, StringComparison.OrdinalIgnoreCase));

            if (!isHomogeneous)
            {
                throw new Exception(errHomogeneity);
            }

            try
            {
                // Background worker thread intercepts the heavy processing execution
                return await Task.Run(() => OfficeBatchToPdfMerger.ConvertAndMerge(
                    inputPaths: inputPaths,
                    newFileName: newFileName,
                    filePathToSave: filePathToSave,
                    libreOfficeExePath: Vars.libreDIR,
                    mode: mode,
                    merge: merge,
                    progress: progress,
                    cancellationToken: cancellationToken
                ), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new Exception(errEngine);
            }
        }

        public static async Task<List<string>> Pdf2ImageCallerAsync(
            string pdfPath,
            string outputPath,
            int dpi,
            int quality,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            // --- CONFIGURABLE VARIABLES ---
            string[] allowedExtensions = { ".pdf" };
            string errNoPdf = "no pdf error";
            string errEngine = "internal engine error";
            // ------------------------------

            // Lightweight validation
            if (!AreExtensionsValid(new[] { pdfPath }, allowedExtensions))
            {
                throw new Exception(errNoPdf);
            }

            try
            {
                // Exception Unrolling happens beautifully when internal engines bubble up out of Task.Run
                return await Task.Run(() => PdfToImageConverter.ConvertPdfToImages(
                    pdfPath: pdfPath,
                    outputPath: outputPath,
                    dpi: dpi,
                    quality: quality,
                    progress: progress,
                    cancellationToken: cancellationToken
                ), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new Exception(errEngine);
            }
        }

        public static async Task<string> PdfMergerCallerAsync(
            string[] pdfPaths,
            string filePathToSave,
            string newFileName,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            // --- CONFIGURABLE VARIABLES ---
            string[] allowedExtensions = { ".pdf" };
            string errNoPdf = "no pdf error";
            string errEngine = "internal engine error";
            // ------------------------------

            // Lightweight validation
            if (!AreExtensionsValid(pdfPaths, allowedExtensions))
            {
                throw new Exception(errNoPdf);
            }

            try
            {
                // Yield the UI thread immediately; merge heavy PDFs in the background
                return await Task.Run(() => PdfMerger.Merge(
                    pdfPaths: pdfPaths,
                    filePathToSave: filePathToSave,
                    newFileName: newFileName,
                    progress: progress,
                    cancellationToken: cancellationToken
                ), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new Exception(errEngine);
            }
        }
    }
}