
///  NEW CODE

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfUtilities;

public static class PdfMerger
{
    private static void ReportProgress(ref int processedFiles, int totalFiles, IProgress<double>? progress)
    {
        if (progress == null) return;
        int currentProcessed = Interlocked.Increment(ref processedFiles);
        progress.Report((double)currentProcessed / totalFiles * 100);
    }

    /// <summary>
    /// Structurally merges multiple PDFs by extracting and appending raw object graphs.
    /// Guarantees zero quality loss, bypasses stream decompression, and handles naming collisions.
    /// </summary>
    /// <param name="pdfPaths">Array of absolute paths to the source PDF files.</param>
    /// <param name="filePathToSave">The directory path where the merged file will be saved.</param>
    /// <param name="newFileName">The desired output file name (e.g., "MergedOutput.pdf").</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token to abort the operation.</param>
    /// <returns>The final absolute path to the successfully saved merged PDF.</returns>
    public static string Merge(
        string[] pdfPaths, 
        string filePathToSave, 
        string newFileName, 
        IProgress<double>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        // 1. Ensure target directory exists
        if (!Directory.Exists(filePathToSave))
        {
            Directory.CreateDirectory(filePathToSave);
        }

        // 2. Format base filename to enforce .pdf extension
        if (!newFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            newFileName += ".pdf";
        }

        string finalPath = Path.Combine(filePathToSave, newFileName);

        // 3. Collision Handling: Append a unique incrementing suffix if the file exists
        if (File.Exists(finalPath))
        {
            string nameWithoutExt = Path.GetFileNameWithoutExtension(newFileName);
            int counter = 1;
            
            do
            {
                finalPath = Path.Combine(filePathToSave, $"{nameWithoutExt}_{counter}.pdf");
                counter++;
            } 
            while (File.Exists(finalPath));
        }

        // 4. Create the blank target document container (Object graph root)
        using var outputDocument = new PdfDocument();

        int processedFiles = 0;
        int totalFiles = pdfPaths.Length;

        try
        {
            // 5. Streaming I/O Pipeline: Process sequentially to prevent RAM bloat
            foreach (string pdfPath in pdfPaths)
            {
                // ZERO COMPROMISE PROTOCOL: Nuke immediately if user requested cancellation
                cancellationToken.ThrowIfCancellationRequested();

                if (!File.Exists(pdfPath))
                {
                    // ZERO COMPROMISE PROTOCOL: One failure kills the whole thing
                    throw new FileNotFoundException($"The source PDF was not found: {pdfPath}");
                }

                // Architectural Blueprint A: Open in Import Mode.
                // This prevents the engine from parsing the /Contents vectors, copies raw compressed
                // streams (FlateDecode) byte-for-byte, and skips all CPU-wasting decompression.
                using var inputDocument = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Import);

                // Architectural Blueprint B: Direct Object Graph Appending.
                // Iterates through the page tree, copying object references structurally without rendering.
                for (int i = 0; i < inputDocument.PageCount; i++)
                {
                    outputDocument.AddPage(inputDocument.Pages[i]);
                }
                
                // The inputDocument is immediately disposed here, freeing its specific file hooks
                // from RAM, while outputDocument safely retains the mapped structural references.

                ReportProgress(ref processedFiles, totalFiles, progress);
            }

            // Check cancellation one last time right before Disk I/O starts
            cancellationToken.ThrowIfCancellationRequested();

            // 6. Stream the final rearranged structure directly to the disk
            using var outputStream = new FileStream(finalPath, FileMode.Create, FileAccess.Write, FileShare.None);
            outputDocument.Save(outputStream, false);

            return finalPath;
        }
        catch
        {
            // NUKE PROTOCOL CLEANUP: If anything failed—cancellation, bad PDF format, locked file, 
            // missing file, or save failure—we ensure there are zero partially merged files left on the disk.
            if (File.Exists(finalPath))
            {
                try 
                { 
                    File.Delete(finalPath); 
                } 
                catch 
                { 
                    /* Suppress delete errors to prioritize surfacing the original crash exception */ 
                }
            }

            // Immediately re-throw the error/cancellation. No bypassing, no partial successes.
            throw;
        }
    }
}