
// using System;
// using System.Collections.Generic;
// using System.IO;
// using DesktopEngine.Sys; // Contains CommandGateway
// using PdfUtilities;      // Contains PdfMerger

// namespace Orchestration
// {
//     public static class OfficeBatchToPdfMerger
//     {
//         /// <summary>
//         /// Orchestrates the batch conversion of strictly homogeneous DOCX or PPTX files to PDF, merging them into one.
//         /// Zero tolerance for mixed file types. Aggressively handles temp sandboxing and cleanup.
//         /// </summary>
//         /// <param name="inputPaths">Array of input file paths (must be ALL docx or ALL pptx).</param>
//         /// <param name="newFileName">Desired name for the final merged PDF.</param>
//         /// <param name="filePathToSave">Final destination directory.</param>
//         /// <param name="libreOfficeExePath">Absolute path to soffice.exe.</param>
//         /// <param name="mode">Must be exactly "docx-pdf" or "pptx-pdf".</param>
//         /// <returns>The final absolute path to the completed, collision-handled merged PDF.</returns>
//         public static string ConvertAndMerge(string[] inputPaths, string newFileName, string filePathToSave, string libreOfficeExePath, string mode)
//         {
//             // 1. Validate basic inputs
//             if (inputPaths == null || inputPaths.Length == 0)
//                 throw new ArgumentException("System Error: No input documents provided for the batch operation.");

//             if (mode != "docx-pdf" && mode != "pptx-pdf")
//                 throw new ArgumentException($"System Error: Unsupported mode '{mode}'. Allowed modes are 'docx-pdf' or 'pptx-pdf'.");

//             // 2. Strict Homogeneity & Mode Enforcement Phase
//             // We lock the expected extension based entirely on the mode requested.
//             string expectedExtension = mode == "docx-pdf" ? ".docx" : ".pptx";

//             foreach (string path in inputPaths)
//             {
//                 string ext = Path.GetExtension(path);
                
//                 // Case-insensitive check to allow .DOCX, .Docx, .pptx, etc.
//                 if (!string.Equals(ext, expectedExtension, StringComparison.OrdinalIgnoreCase))
//                 {
//                     // FULL ABORT: A violation is detected before we even waste CPU cycles or touch the disk.
//                     throw new ArgumentException(
//                         $"FULL ABORT: Sandbox rejected the payload. The mode is set to '{mode}', " +
//                         $"which strictly requires ALL files to be '{expectedExtension}'. " +
//                         $"Found illegal file type in batch: {path}"
//                     );
//                 }
//             }

//             // 3. Create the Master Volatile Sandbox for this specific orchestration run
//             // We use a completely isolated temp folder so multi-threaded calls to this orchestrator don't cross-contaminate.
//             string masterSandboxDir = Path.Combine(Path.GetTempPath(), $"MasterOrchestration_{Guid.NewGuid():N}");
//             Directory.CreateDirectory(masterSandboxDir);

//             try
//             {
//                 List<string> tempPdfPaths = new List<string>();

//                 // 4. Gateway Phase: Route every single verified file through the LibreOffice CommandGateway
//                 foreach (string docPath in inputPaths)
//                 {
//                     // If they passed the exact same file multiple times, we assign a unique internal GUID name 
//                     // inside the sandbox to prevent LibreOffice / OS I/O locking conflicts.
//                     string internalTempName = $"Converted_{Guid.NewGuid():N}.pdf";

//                     // The Gateway takes care of the CPU heartbeat, flatlines, and sandbox isolation per file
//                     string convertedPdfPath = CommandGateway.Convert(
//                         libreOfficeExePath: libreOfficeExePath,
//                         filepath: docPath,
//                         newFilename: internalTempName,
//                         folderPath: masterSandboxDir,
//                         mode: mode // "docx-pdf" or "pptx-pdf" is passed down
//                     );

//                     tempPdfPaths.Add(convertedPdfPath);
//                 }

//                 // 5. Merge Phase: Stream the successfully converted PDFs into one
//                 // PdfMerger already has its own collision handler for the final destination.
//                 string finalMergedPdf = PdfMerger.Merge(
//                     pdfPaths: tempPdfPaths.ToArray(), 
//                     filePathToSave: filePathToSave, 
//                     newFileName: newFileName
//                 );

//                 return finalMergedPdf; // Success delivery
//             }
//             finally
//             {
//                 // 6. THE NUKE: Unconditional Cleanup
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
        /// Orchestrates the batch conversion of strictly homogeneous DOCX or PPTX files to PDF.
        /// Zero tolerance for mixed file types. Aggressively handles temp sandboxing and cleanup.
        /// </summary>
        /// <param name="inputPaths">Array of input file paths (must be ALL docx or ALL pptx).</param>
        /// <param name="newFileName">Desired name for the final merged PDF (Ignored if merge = 0).</param>
        /// <param name="filePathToSave">Final destination directory.</param>
        /// <param name="libreOfficeExePath">Absolute path to soffice.exe.</param>
        /// <param name="mode">Must be exactly "docx-pdf" or "pptx-pdf".</param>
        /// <param name="merge">Strictly 1 to merge all into one PDF. Strictly 0 to output individually. No default value.</param>
        /// <returns>An array containing the absolute paths to the completed, collision-handled PDF(s).</returns>
        public static string[] ConvertAndMerge(string[] inputPaths, string newFileName, string filePathToSave, string libreOfficeExePath, string mode, int merge)
        {
            // 1. Validate basic inputs
            if (inputPaths == null || inputPaths.Length == 0)
                throw new ArgumentException("System Error: No input documents provided for the batch operation.");

            if (mode != "docx-pdf" && mode != "pptx-pdf")
                throw new ArgumentException($"System Error: Unsupported mode '{mode}'. Allowed modes are 'docx-pdf' or 'pptx-pdf'.");

            if (merge != 0 && merge != 1)
                throw new ArgumentException("System Error: The 'merge' parameter rigidly expects 1 or 0.");

            // 2. Strict Homogeneity & Mode Enforcement Phase
            string expectedExtension = mode == "docx-pdf" ? ".docx" : ".pptx";

            foreach (string path in inputPaths)
            {
                string ext = Path.GetExtension(path);
                
                if (!string.Equals(ext, expectedExtension, StringComparison.OrdinalIgnoreCase))
                {
                    // FULL ABORT
                    throw new ArgumentException(
                        $"FULL ABORT: Sandbox rejected the payload. The mode is set to '{mode}', " +
                        $"which strictly requires ALL files to be '{expectedExtension}'. " +
                        $"Found illegal file type in batch: {path}"
                    );
                }
            }

            // 3. Create the Master Volatile Sandbox
            string masterSandboxDir = Path.Combine(Path.GetTempPath(), $"MasterOrchestration_{Guid.NewGuid():N}");
            Directory.CreateDirectory(masterSandboxDir);

            try
            {
                List<string> tempPdfPaths = new List<string>();
                List<string> originalBaseNames = new List<string>();

                // 4. Gateway Phase: Route every single verified file through the CommandGateway
                foreach (string docPath in inputPaths)
                {
                    string internalTempName = $"Converted_{Guid.NewGuid():N}.pdf";

                    string convertedPdfPath = CommandGateway.Convert(
                        libreOfficeExePath: libreOfficeExePath,
                        filepath: docPath,
                        newFilename: internalTempName,
                        folderPath: masterSandboxDir,
                        mode: mode 
                    );

                    tempPdfPaths.Add(convertedPdfPath);
                    originalBaseNames.Add(Path.GetFileNameWithoutExtension(docPath));
                }

                // 5. Output Routing Phase (Merge vs Dump)
                if (merge == 1)
                {
                    // Stream the successfully converted sandbox PDFs into one
                    string finalMergedPdf = PdfMerger.Merge(
                        pdfPaths: tempPdfPaths.ToArray(), 
                        filePathToSave: filePathToSave, 
                        newFileName: newFileName
                    );

                    return new string[] { finalMergedPdf }; // Success delivery (Single File Array)
                }
                else
                {
                    // Dump the files preserving original names via Collision Handler
                    List<string> finalIndividualPdfs = new List<string>();

                    for (int i = 0; i < tempPdfPaths.Count; i++)
                    {
                        string tempSandboxFile = tempPdfPaths[i];
                        string baseName = originalBaseNames[i];

                        // Get an absolutely collision-free path in the intended directory
                        string safeFinalPath = GetCollisionFreePath(filePathToSave, baseName, ".pdf");

                        // Atomic move from Sandbox to Target directory
                        File.Move(tempSandboxFile, safeFinalPath);
                        finalIndividualPdfs.Add(safeFinalPath);
                    }

                    return finalIndividualPdfs.ToArray(); // Success delivery (Multiple File Array)
                }
            }
            finally
            {
                // 6. THE NUKE: Unconditional Cleanup
                if (Directory.Exists(masterSandboxDir))
                {
                    try 
                    { 
                        Directory.Delete(masterSandboxDir, true); 
                    } 
                    catch 
                    { 
                        // Suppress I/O teardown exceptions
                    }
                }
            }
        }

        /// <summary>
        /// Aggressive collision handler. Guarantees no overwrites by appending (1), (2), etc.
        /// </summary>
        private static string GetCollisionFreePath(string directoryPath, string baseName, string extension)
        {
            string currentPath = Path.Combine(directoryPath, $"{baseName}{extension}");
            int counter = 1;

            while (File.Exists(currentPath))
            {
                currentPath = Path.Combine(directoryPath, $"{baseName}({counter}){extension}");
                counter++;
            }

            return currentPath;
        }
    }
}