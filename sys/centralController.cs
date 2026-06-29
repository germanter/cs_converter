

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
        private static int _isRunning = 0;
        private static int _nuke = 0; // 0 = false, 1 = true
        private static CancellationTokenSource _nukeCts = new CancellationTokenSource();
        private static readonly object _nukeLock = new object();

        public static bool isRunning
        {
            get => _isRunning == 1;
            set => Interlocked.Exchange(ref _isRunning, value ? 1 : 0);
        }

        public static bool nuke
        {
            get => Volatile.Read(ref _nuke) == 1;
            set
            {
                lock (_nukeLock)
                {
                    bool prevValue = _nuke == 1;
                    if (prevValue == value) return;

                    _nuke = value ? 1 : 0;

                    if (value)
                    {
                        _nukeCts.Cancel();
                    }
                    else
                    {
                        // Safely dispose the cancelled CTS and instantiate a new one for future jobs
                        var oldCts = _nukeCts;
                        _nukeCts = new CancellationTokenSource();
                        try
                        {
                            oldCts.Dispose();
                        }
                        catch
                        {
                            // Suppress potential disposal issues
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Combines the external CancellationToken with our internal nuke cancellation source.
        /// </summary>
        private static CancellationTokenSource CreateLinkedCts(CancellationToken externalToken)
        {
            lock (_nukeLock)
            {
                return CancellationTokenSource.CreateLinkedTokenSource(externalToken, _nukeCts.Token);
            }
        }

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
            if (nuke || Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0) 
                return string.Empty;

            using var linkedCts = CreateLinkedCts(cancellationToken);

            try
            {
                string taskType = "image2pdf";
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".ico", ".bmp", ".tiff", ".tif", ".tga", ".psd" };
                string errUnsupportedType = "unsupported image type";
                string errEngine = "internal engine error";

                // Lightweight validation runs instantly on the calling UI thread
                if (!AreExtensionsValid(images.Select(img => img.FilePath), allowedExtensions))
                {
                    Logger.Log(taskType, "", "fail");
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
                        cancellationToken: linkedCts.Token
                    ), linkedCts.Token);

                    Logger.Log(taskType, result, "success"); // result is a single string here
                    return result;
                }
                catch (OperationCanceledException)
                {
                    Logger.Log(taskType, "", "fail");
                    throw; // Bubble up intentional user/nuke cancellations unchanged natively
                }
                catch (Exception)
                {
                    Logger.Log(taskType, "", "fail");
                    throw new Exception(errEngine);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _isRunning, 0);
            }
        }

        public static async Task<string> ImageConverterCallerAsync(
            string[] sourceImages,
            string targetFormat,
            string outputPath,
            string? filename, // Added the filename parameter
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (nuke || Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0) 
                return string.Empty;

            using var linkedCts = CreateLinkedCts(cancellationToken);

            try
            {
                string taskType = "imageconverter";
                string[] allowedExtensions = { ".jpg", ".jpeg", ".ico", ".png", ".webp", ".bmp", ".tiff", ".tif" };
                string errUnsupportedType = "unsupported image type";
                string errEngine = "internal engine error";

                if (!AreExtensionsValid(sourceImages, allowedExtensions))
                {
                    Logger.Log(taskType, "", "fail");
                    throw new Exception(errUnsupportedType);
                }

                try
                {
                    var result = await UltimateImageConverter.ConvertImagesAsync(
                        sourceImages,
                        targetFormat,
                        outputPath,
                        filename, // Passed the filename parameter to the engine
                        progress,
                        linkedCts.Token
                    );

                    string logPath = GetLogPath(result);
                    Logger.Log(taskType, logPath, "success");
                    return logPath;
                }
                catch (OperationCanceledException)
                {
                    Logger.Log(taskType, "", "fail");
                    throw;
                }
                catch (Exception)
                {
                    Logger.Log(taskType, "", "fail");
                    throw new Exception(errEngine);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _isRunning, 0);
            }
        }

        public static async Task<string> Office2PdfCallerAsync(
            string[] inputPaths,
            string newFileName,
            string filePathToSave,
            string mode,
            int merge,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (nuke || Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0) 
                return string.Empty;

            using var linkedCts = CreateLinkedCts(cancellationToken);

            try
            {
                string taskType = "office2pdf";
                string errHomogeneity = "homogenity error";
                string errEngine = "internal engine error";

                string expectedExtension = mode == "docx-pdf" ? ".docx" : 
                                           mode == "pptx-pdf" ? ".pptx" : 
                                           string.Empty;

                if (string.IsNullOrEmpty(expectedExtension))
                {
                    Logger.Log(taskType, "", "fail");
                    throw new Exception(errHomogeneity);
                }

                bool isHomogeneous = inputPaths.All(path => 
                    !string.IsNullOrWhiteSpace(path) && 
                    Path.GetExtension(path).Equals(expectedExtension, StringComparison.OrdinalIgnoreCase));

                if (!isHomogeneous)
                {
                    Logger.Log(taskType, "", "fail");
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
                        cancellationToken: linkedCts.Token
                    ), linkedCts.Token);

                    string logPath = GetLogPath(result);
                    Logger.Log(taskType, logPath, "success");
                    return logPath;
                }
                catch (OperationCanceledException)
                {
                    Logger.Log(taskType, "", "fail");
                    throw;
                }
                catch (Exception)
                {
                    Logger.Log(taskType, "", "fail");
                    throw new Exception(errEngine);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _isRunning, 0);
            }
        }

        public static async Task<string> Pdf2ImageCallerAsync(
            string pdfPath,
            string outputPath,
            string? filename, // Added the filename parameter
            int quality,
            int dpi, // Added dpi parameter
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (nuke || Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0) 
                return string.Empty;

            using var linkedCts = CreateLinkedCts(cancellationToken);

            try
            {
                string taskType = "pdf2image";
                string[] allowedExtensions = { ".pdf" };
                string errNoPdf = "no pdf error";
                string errEngine = "internal engine error";

                if (!AreExtensionsValid(new[] { pdfPath }, allowedExtensions))
                {
                    Logger.Log(taskType, "", "fail");
                    throw new Exception(errNoPdf);
                }

                // Check if the value is 200 or 300, otherwise fallback to 200
                int validatedDpi = (dpi == 200 || dpi == 300) ? dpi : 200;

                try
                {
                    var result = await Task.Run(() => PdfToImageConverter.ConvertPdfToImages(
                        pdfPath: pdfPath,
                        outputPath: outputPath,
                        filename: filename, // Passed to the engine
                        dpi: validatedDpi, // Passed the validated DPI
                        quality: quality,
                        progress: progress,
                        cancellationToken: linkedCts.Token
                    ), linkedCts.Token);

                    string logPath = GetLogPath(result);
                    Logger.Log(taskType, logPath, "success");
                    return logPath;
                }
                catch (OperationCanceledException)
                {
                    Logger.Log(taskType, "", "fail");
                    throw;
                }
                catch (Exception)
                {
                    Logger.Log(taskType, "", "fail");
                    throw new Exception(errEngine);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _isRunning, 0);
            }
        }

        public static async Task<string> PdfMergerCallerAsync(
            string[] pdfPaths,
            string filePathToSave,
            string newFileName,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (nuke || Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0) 
                return string.Empty;

            using var linkedCts = CreateLinkedCts(cancellationToken);

            try
            {
                string taskType = "pdfmerger";
                string[] allowedExtensions = { ".pdf" };
                string errNoPdf = "no pdf error";
                string errEngine = "internal engine error";

                if (!AreExtensionsValid(pdfPaths, allowedExtensions))
                {
                    Logger.Log(taskType, "", "fail");
                    throw new Exception(errNoPdf);
                }

                try
                {
                    string result = await Task.Run(() => PdfMerger.Merge(
                        pdfPaths: pdfPaths,
                        filePathToSave: filePathToSave,
                        newFileName: newFileName,
                        progress: progress,
                        cancellationToken: linkedCts.Token
                    ), linkedCts.Token);

                    Logger.Log(taskType, result, "success"); // single string returned natively
                    return result;
                }
                catch (OperationCanceledException)
                {
                    Logger.Log(taskType, "", "fail");
                    throw;
                }
                catch (Exception)
                {
                    Logger.Log(taskType, "", "fail");
                    throw new Exception(errEngine);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _isRunning, 0);
            }
        }
    }
}