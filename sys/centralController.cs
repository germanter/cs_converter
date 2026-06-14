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










/// WRAPPED NEW
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
using AppLogger; // Added for the Logger

namespace CentralGateway
{
    public static class CentralController
    {
        /// <summary>
        /// DRY Helper to validate file extensions rigidly.
        /// </summary>
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

        /// <summary>
        /// DRY Helper for the Logger to determine if it should return a single file path or a parent directory.
        /// </summary>
        private static string GetLogPath(IEnumerable<string> paths)
        {
            if (paths == null) return "";
            
            var list = paths.ToList();
            if (list.Count == 0) return "";
            
            // If it's a single file, return the full path
            if (list.Count == 1) return list[0]; 

            // If multiple files, return the parent folder path (ensuring it has a trailing slash for aesthetics)
            string directory = Path.GetDirectoryName(list[0]) ?? "";
            if (!string.IsNullOrEmpty(directory) && 
                !directory.EndsWith(Path.DirectorySeparatorChar.ToString()) && 
                !directory.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                directory += Path.DirectorySeparatorChar;
            }
            return directory;
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
            string taskType = "image2pdf";
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".ico", ".bmp", ".tiff", ".tif", ".tga", ".psd" };
            string errUnsupportedType = "unsupported image type";
            string errEngine = "internal engine error";

            // Lightweight validation runs instantly on the calling UI thread
            if (!AreExtensionsValid(images.Select(img => img.FilePath), allowedExtensions))
            {
                if (Vars.openLog) Logger.Log(taskType, "", "fail");
                throw new Exception(errUnsupportedType);
            }

            try
            {
                // UI Thread Yields Instantly: Handoff heavy processing to the thread-pool
                string result = await Task.Run(() => ImageToPdfEngine.ConvertToPdf(
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

                if (Vars.openLog) Logger.Log(taskType, result, "success"); // result is a single string here
                return result;
            }
            catch (OperationCanceledException)
            {
                if (Vars.openLog) Logger.Log(taskType, "", "fail");
                throw; // Bubble up intentional user cancellations unchanged natively
            }
            catch (Exception)
            {
                if (Vars.openLog) Logger.Log(taskType, "", "fail");
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
            string taskType = "imageconverter";
            string[] allowedExtensions = { ".jpg", ".jpeg", ".ico", ".png", ".webp", ".bmp", ".tiff", ".tif" };
            string errUnsupportedType = "unsupported image type";
            string errEngine = "internal engine error";

            if (!AreExtensionsValid(sourceImages, allowedExtensions))
            {
                if (Vars.openLog) Logger.Log(taskType, "", "fail");
                throw new Exception(errUnsupportedType);
            }

            try
            {
                var result = await UltimateImageConverter.ConvertImagesAsync(
                    sourceImages,
                    targetFormat,
                    outputPath,
                    progress,
                    cancellationToken
                );

                if (Vars.openLog) Logger.Log(taskType, GetLogPath(result), "success");
                return result;
            }
            catch (OperationCanceledException)
            {
                if (Vars.openLog) Logger.Log(taskType, "", "fail");
                throw;
            }
            catch (Exception)
            {
                if (Vars.openLog) Logger.Log(taskType, "", "fail");
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
            string taskType = "office2pdf";
            string errHomogeneity = "homogenity error";
            string errEngine = "internal engine error";

            string expectedExtension = mode == "docx-pdf" ? ".docx" : 
                                       mode == "pptx-pdf" ? ".pptx" : 
                                       string.Empty;

            if (string.IsNullOrEmpty(expectedExtension))
            {
                if (Vars.openLog) Logger.Log(taskType, "", "fail");
                throw new Exception(errHomogeneity);
            }

            bool isHomogeneous = inputPaths.All(path => 
                !string.IsNullOrWhiteSpace(path) && 
                Path.GetExtension(path).Equals(expectedExtension, StringComparison.OrdinalIgnoreCase));

            if (!isHomogeneous)
            {
                if (Vars.openLog) Logger.Log(taskType, "", "fail");
                throw new Exception(errHomogeneity);
            }

            try
            {
                string[] result = await Task.Run(() => OfficeBatchToPdfMerger.ConvertAndMerge(
                    inputPaths: inputPaths,
                    newFileName: newFileName,
                    filePathToSave: filePathToSave,
                    libreOfficeExePath: Vars.libreDIR,
                    mode: mode,
                    merge: merge,
                    progress: progress,
                    cancellationToken: cancellationToken
                ), cancellationToken);

                if (Vars.openLog) Logger.Log(taskType, GetLogPath(result), "success");
                return result;
            }
            catch (OperationCanceledException)
            {
                if (Vars.openLog) Logger.Log(taskType, "", "fail");
                throw;
            }
            catch (Exception)
            {
                if (Vars.openLog) Logger.Log(taskType, "", "fail");
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
            string taskType = "pdf2image";
            string[] allowedExtensions = { ".pdf" };
            string errNoPdf = "no pdf error";
            string errEngine = "internal engine error";

            if (!AreExtensionsValid(new[] { pdfPath }, allowedExtensions))
            {
                if (Vars.openLog) Logger.Log(taskType, "", "fail");
                throw new Exception(errNoPdf);
            }

            try
            {
                var result = await Task.Run(() => PdfToImageConverter.ConvertPdfToImages(
                    pdfPath: pdfPath,
                    outputPath: outputPath,
                    dpi: dpi,
                    quality: quality,
                    progress: progress,
                    cancellationToken: cancellationToken
                ), cancellationToken);

                if (Vars.openLog) Logger.Log(taskType, GetLogPath(result), "success");
                return result;
            }
            catch (OperationCanceledException)
            {
                if (Vars.openLog) Logger.Log(taskType, "", "fail");
                throw;
            }
            catch (Exception)
            {
                if (Vars.openLog) Logger.Log(taskType, "", "fail");
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
            string taskType = "pdfmerger";
            string[] allowedExtensions = { ".pdf" };
            string errNoPdf = "no pdf error";
            string errEngine = "internal engine error";

            if (!AreExtensionsValid(pdfPaths, allowedExtensions))
            {
                if (Vars.openLog) Logger.Log(taskType, "", "fail");
                throw new Exception(errNoPdf);
            }

            try
            {
                string result = await Task.Run(() => PdfMerger.Merge(
                    pdfPaths: pdfPaths,
                    filePathToSave: filePathToSave,
                    newFileName: newFileName,
                    progress: progress,
                    cancellationToken: cancellationToken
                ), cancellationToken);

                if (Vars.openLog) Logger.Log(taskType, result, "success"); // single string returned natively
                return result;
            }
            catch (OperationCanceledException)
            {
                if (Vars.openLog) Logger.Log(taskType, "", "fail");
                throw;
            }
            catch (Exception)
            {
                if (Vars.openLog) Logger.Log(taskType, "", "fail");
                throw new Exception(errEngine);
            }
        }
    }
}