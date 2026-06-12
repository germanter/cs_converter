// using System;
// using System.Collections.Generic;
// using System.IO;
// using DesktopEngine.Sys; // Contains CommandGateway
// using PdfUtilities;      // Contains PdfMerger

// namespace Orchestration
// {
//     public static class DocxMerger2Pdf
//     {
//         /// <summary>
//         /// Orchestrates the conversion of multiple DOCX files to PDFs and merges them into a single file.
//         /// Aggressively handles duplicates, file collisions, and guarantees volatile temp cleanup.
//         /// </summary>
//         /// <param name="docxPaths">Array of input file paths (even if they pass exact same file 6 times or just 1 file).</param>
//         /// <param name="newFileName">Desired name for the final merged PDF.</param>
//         /// <param name="filePathToSave">Final destination directory.</param>
//         /// <param name="libreOfficeExePath">Absolute path to soffice.exe.</param>
//         /// <returns>The final absolute path to the completed, collision-handled merged PDF.</returns>
//         public static string ConvertAndMerge(string[] docxPaths, string newFileName, string filePathToSave, string libreOfficeExePath)
//         {
//             // 1. Validate inputs
//             if (docxPaths == null || docxPaths.Length == 0)
//                 throw new ArgumentException("System Error: No input documents provided for the batch operation.");

//             // 2. Create the Master Volatile Sandbox for this specific orchestration run
//             // We use a completely isolated temp folder so multi-threaded calls to this orchestrator don't cross-contaminate.
//             string masterSandboxDir = Path.Combine(Path.GetTempPath(), $"MasterOrchestration_{Guid.NewGuid():N}");
//             Directory.CreateDirectory(masterSandboxDir);

//             try
//             {
//                 List<string> tempPdfPaths = new List<string>();

//                 // 3. Gateway Phase: Route every single file through the LibreOffice CommandGateway
//                 foreach (string docPath in docxPaths)
//                 {
//                     // If they passed the exact same file 6 times, we assign a unique internal GUID name 
//                     // inside the sandbox to prevent LibreOffice / OS I/O locking conflicts.
//                     string internalTempName = $"Converted_{Guid.NewGuid():N}.pdf";

//                     // The Gateway takes care of the CPU heartbeat, flatlines, and sandbox isolation per file
//                     string convertedPdfPath = CommandGateway.Convert(
//                         libreOfficeExePath: libreOfficeExePath,
//                         filepath: docPath,
//                         newFilename: internalTempName,
//                         folderPath: masterSandboxDir,
//                         mode: "docx-pdf"
//                     );

//                     tempPdfPaths.Add(convertedPdfPath);
//                 }

//                 // 4. Merge Phase: Stream the successfully converted PDFs into one
//                 // PdfMerger already has its own collision handler for the final destination, so we just feed it.
//                 // Even if tempPdfPaths has exactly 1 file, PdfMerger safely handles it.
//                 string finalMergedPdf = PdfMerger.Merge(
//                     pdfPaths: tempPdfPaths.ToArray(), 
//                     filePathToSave: filePathToSave, 
//                     newFileName: newFileName
//                 );

//                 return finalMergedPdf; // Success delivery
//             }
//             finally
//             {
//                 // 5. THE NUKE: Unconditional Cleanup
//                 // This executes no matter what: Success, LibreOffice crash, Merger failure, or OS exception.
//                 if (Directory.Exists(masterSandboxDir))
//                 {
//                     try 
//                     { 
//                         Directory.Delete(masterSandboxDir, true); 
//                     } 
//                     catch 
//                     { 
//                         // Suppress I/O teardown exceptions so we don't mask the primary Exception 
//                         // if the pipeline blew up earlier in the try block.
//                     }
//                 }
//             }
//         }
//     }
// }






























using System;
using System.Collections.Generic;
using System.IO;
using DesktopEngine.Sys; // Contains CommandGateway
using PdfUtilities;      // Contains PdfMerger

namespace Orchestration
{
    public static class OfficeBatchToPdfMerger
    {
        /// <summary>
        /// Orchestrates the batch conversion of strictly homogeneous DOCX or PPTX files to PDF, merging them into one.
        /// Zero tolerance for mixed file types. Aggressively handles temp sandboxing and cleanup.
        /// </summary>
        /// <param name="inputPaths">Array of input file paths (must be ALL docx or ALL pptx).</param>
        /// <param name="newFileName">Desired name for the final merged PDF.</param>
        /// <param name="filePathToSave">Final destination directory.</param>
        /// <param name="libreOfficeExePath">Absolute path to soffice.exe.</param>
        /// <param name="mode">Must be exactly "docx-pdf" or "pptx-pdf".</param>
        /// <returns>The final absolute path to the completed, collision-handled merged PDF.</returns>
        public static string ConvertAndMerge(string[] inputPaths, string newFileName, string filePathToSave, string libreOfficeExePath, string mode)
        {
            // 1. Validate basic inputs
            if (inputPaths == null || inputPaths.Length == 0)
                throw new ArgumentException("System Error: No input documents provided for the batch operation.");

            if (mode != "docx-pdf" && mode != "pptx-pdf")
                throw new ArgumentException($"System Error: Unsupported mode '{mode}'. Allowed modes are 'docx-pdf' or 'pptx-pdf'.");

            // 2. Strict Homogeneity & Mode Enforcement Phase
            // We lock the expected extension based entirely on the mode requested.
            string expectedExtension = mode == "docx-pdf" ? ".docx" : ".pptx";

            foreach (string path in inputPaths)
            {
                string ext = Path.GetExtension(path);
                
                // Case-insensitive check to allow .DOCX, .Docx, .pptx, etc.
                if (!string.Equals(ext, expectedExtension, StringComparison.OrdinalIgnoreCase))
                {
                    // FULL ABORT: A violation is detected before we even waste CPU cycles or touch the disk.
                    throw new ArgumentException(
                        $"FULL ABORT: Sandbox rejected the payload. The mode is set to '{mode}', " +
                        $"which strictly requires ALL files to be '{expectedExtension}'. " +
                        $"Found illegal file type in batch: {path}"
                    );
                }
            }

            // 3. Create the Master Volatile Sandbox for this specific orchestration run
            // We use a completely isolated temp folder so multi-threaded calls to this orchestrator don't cross-contaminate.
            string masterSandboxDir = Path.Combine(Path.GetTempPath(), $"MasterOrchestration_{Guid.NewGuid():N}");
            Directory.CreateDirectory(masterSandboxDir);

            try
            {
                List<string> tempPdfPaths = new List<string>();

                // 4. Gateway Phase: Route every single verified file through the LibreOffice CommandGateway
                foreach (string docPath in inputPaths)
                {
                    // If they passed the exact same file multiple times, we assign a unique internal GUID name 
                    // inside the sandbox to prevent LibreOffice / OS I/O locking conflicts.
                    string internalTempName = $"Converted_{Guid.NewGuid():N}.pdf";

                    // The Gateway takes care of the CPU heartbeat, flatlines, and sandbox isolation per file
                    string convertedPdfPath = CommandGateway.Convert(
                        libreOfficeExePath: libreOfficeExePath,
                        filepath: docPath,
                        newFilename: internalTempName,
                        folderPath: masterSandboxDir,
                        mode: mode // "docx-pdf" or "pptx-pdf" is passed down
                    );

                    tempPdfPaths.Add(convertedPdfPath);
                }

                // 5. Merge Phase: Stream the successfully converted PDFs into one
                // PdfMerger already has its own collision handler for the final destination.
                string finalMergedPdf = PdfMerger.Merge(
                    pdfPaths: tempPdfPaths.ToArray(), 
                    filePathToSave: filePathToSave, 
                    newFileName: newFileName
                );

                return finalMergedPdf; // Success delivery
            }
            finally
            {
                // 6. THE NUKE: Unconditional Cleanup
                // This executes no matter what: Success, LibreOffice crash, Merger failure, or OS exception.
                if (Directory.Exists(masterSandboxDir))
                {
                    try 
                    { 
                        Directory.Delete(masterSandboxDir, true); 
                    } 
                    catch 
                    { 
                        // Suppress I/O teardown exceptions so we don't mask the primary Exception 
                        // if the pipeline blew up earlier in the try block.
                    }
                }
            }
        }
    }
}