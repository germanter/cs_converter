using System;
using System.Collections.Generic;
using System.IO;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfUtilities;

public static class PdfMerger
{
    /// <summary>
    /// Structurally merges multiple PDFs by extracting and appending raw object graphs.
    /// Guarantees zero quality loss, bypasses stream decompression, and handles naming collisions.
    /// </summary>
    /// <param name="pdfPaths">Array of absolute paths to the source PDF files.</param>
    /// <param name="filePathToSave">The directory path where the merged file will be saved.</param>
    /// <param name="newFileName">The desired output file name (e.g., "MergedOutput.pdf").</param>
    /// <returns>The final absolute path to the successfully saved merged PDF.</returns>
    public static string Merge(string[] pdfPaths, string filePathToSave, string newFileName)
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

        // 5. Streaming I/O Pipeline: Process sequentially to prevent RAM bloat
        foreach (string pdfPath in pdfPaths)
        {
            if (!File.Exists(pdfPath))
            {
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
        }

        // 6. Stream the final rearranged structure directly to the disk
        using var outputStream = new FileStream(finalPath, FileMode.Create, FileAccess.Write, FileShare.None);
        outputDocument.Save(outputStream, false);

        return finalPath;
    }
}