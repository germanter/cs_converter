using System;  //// FIRST IMAGE TO IMAGE
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// 1. Validate Command Line Arguments
if (args.Length < 3)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("⚠️ Missing Arguments!");
    Console.WriteLine("Usage:   dotnet run -- <format> <output_directory> <image1> <image2> ...");
    Console.WriteLine("Example: dotnet run -- webp ./ConvertedImages ./myimage.png ./myphoto.jpg");
    Console.ResetColor();
    return;
}

string targetFormat = args[0];
string outputPath = args[1];

// Extract all trailing image paths from argument index 2 through the end
string[] sourceImages = args[2..];

Console.WriteLine($"🚀 Starting conversion of {sourceImages.Length} image(s) to '{targetFormat.ToUpperInvariant()}'...\n");

// 2. Wire Up the Progress Reporter (Leveraging your engine's IProgress implementation)
var progressReporter = new Progress<double>(percent =>
{
    // The "\r" characters forces the console cursor back to the start of the line 
    // to provide a smooth, in-place percentage counter.
    Console.Write($"\r🔄 Progress: [{percent:F1}%] Processing assets...");
});

// 3. Graceful Cancellation Handling (Tied directly to your engine's CancellationToken)
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\n🛑 Cancellation requested! Halting execution safely...");
    Console.ResetColor();
    
    cts.Cancel();
    e.Cancel = true; // Prevents the operating system from abruptly killing the process
};

try
{
    // 4. Execute the Beast Mode Async Image Converter
    var convertedFiles = await UltimateImageConverter.ConvertImagesAsync(
        sourceImages, 
        targetFormat, 
        outputPath, 
        progressReporter, 
        cts.Token
    );

    // 5. Output Processing Results
    Console.WriteLine("\n\n✨ --- CONVERSION COMPLETE --- ✨");
    
    if (convertedFiles.Count == sourceImages.Length)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ All images successfully processed: {convertedFiles.Count}/{sourceImages.Length}\n");
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠️ Completed with skipped files or errors: {convertedFiles.Count}/{sourceImages.Length}\n");
    }
    Console.ResetColor();

    foreach (var path in convertedFiles)
    {
        Console.WriteLine($"  [DONE] -> {path}");
    }
}
catch (OperationCanceledException)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("\n❌ Operation aborted by the user.");
    Console.ResetColor();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n❌ Critical System Failure: {ex.Message}");
    Console.ResetColor();
}












// SECOND IMAGE TO PDF

// using ImageToPdfApp;

// Console.WriteLine("=== High-Tier Image to PDF Converter ===");

// try
// {
//     List<ImageInput> images =
//     [
//         new("C:/Users/GERMANTATE/Downloads/file_example_TIFF_1MB.tiff"),
// // --- Batch 1 (1-18) ---
//     new("C:/Users/GERMANTATE/Downloads/file_example_JPG_1MB.jpg"),
//     new("C:/Users/GERMANTATE/Downloads/file_example_TIFF_1MB.tiff", RotationSteps.Right90),
//     new("C:/Users/GERMANTATE/Downloads/file_example_WEBP_1500kB.webp", RotationSteps.Left270),
//     new("C:/Users/GERMANTATE/Downloads/sample-5.webp", RotationSteps.UpsideDown180),
//     new("C:/Users/GERMANTATE/Downloads/sample-4.webp"),
//     new("C:/Users/GERMANTATE/Downloads/sample-3.webp", RotationSteps.Right90),
//     new("C:/Users/GERMANTATE/Downloads/sample-2.webp"),
//     new("C:/Users/GERMANTATE/Downloads/sample-1.webp", RotationSteps.UpsideDown180),
//     new("C:/Users/GERMANTATE/Downloads/sample-5.png", RotationSteps.Left270),
//     new("C:/Users/GERMANTATE/Downloads/sample-4.png"),
//     new("C:/Users/GERMANTATE/Downloads/sample-3.png", RotationSteps.Right90),
//     new("C:/Users/GERMANTATE/Downloads/sample-2.png"),
//     new("C:/Users/GERMANTATE/Downloads/sample-1.png", RotationSteps.UpsideDown180),
//     new("C:/Users/GERMANTATE/Downloads/sample-1.jpg", RotationSteps.Left270),
//     new("C:/Users/GERMANTATE/Downloads/sample-2.jpg"),
//     new("C:/Users/GERMANTATE/Downloads/sample-3.jpg", RotationSteps.Right90),
//     new("C:/Users/GERMANTATE/Downloads/sample-4.jpg"),
//     new("C:/Users/GERMANTATE/Downloads/sample-5.jpg", RotationSteps.UpsideDown180),
    
//     // --- Batch 2 Duplicates (19-36) ---
//     new("C:/Users/GERMANTATE/Downloads/file_example_JPG_1MB.jpg", RotationSteps.Right90),
//     new("C:/Users/GERMANTATE/Downloads/file_example_TIFF_1MB.tiff", RotationSteps.Left270),
//     new("C:/Users/GERMANTATE/Downloads/file_example_WEBP_1500kB.webp", RotationSteps.UpsideDown180),
//     new("C:/Users/GERMANTATE/Downloads/sample-5.webp"),
//     new("C:/Users/GERMANTATE/Downloads/sample-4.webp", RotationSteps.Right90),
//     new("C:/Users/GERMANTATE/Downloads/sample-3.webp"),
//     new("C:/Users/GERMANTATE/Downloads/sample-2.webp", RotationSteps.UpsideDown180),
//     new("C:/Users/GERMANTATE/Downloads/sample-1.webp", RotationSteps.Left270),
//     new("C:/Users/GERMANTATE/Downloads/sample-5.png"),
//     new("C:/Users/GERMANTATE/Downloads/sample-4.png", RotationSteps.Right90),
//     new("C:/Users/GERMANTATE/Downloads/sample-3.png"),
//     new("C:/Users/GERMANTATE/Downloads/sample-2.png", RotationSteps.UpsideDown180),
//     new("C:/Users/GERMANTATE/Downloads/sample-1.png", RotationSteps.Left270),
//     new("C:/Users/GERMANTATE/Downloads/sample-1.jpg"),
//     new("C:/Users/GERMANTATE/Downloads/sample-2.jpg", RotationSteps.Right90),
//     new("C:/Users/GERMANTATE/Downloads/sample-3.jpg"),
//     new("C:/Users/GERMANTATE/Downloads/sample-4.jpg", RotationSteps.UpsideDown180),
//     new("C:/Users/GERMANTATE/Downloads/sample-5.jpg", RotationSteps.Left270),
    
//     // --- Batch 3 Fillers (37-50) ---
//     new("C:/Users/GERMANTATE/Downloads/file_example_JPG_1MB.jpg", RotationSteps.UpsideDown180),
//     new("C:/Users/GERMANTATE/Downloads/file_example_TIFF_1MB.tiff"),
//     new("C:/Users/GERMANTATE/Downloads/file_example_WEBP_1500kB.webp", RotationSteps.Right90),
//     new("C:/Users/GERMANTATE/Downloads/sample-5.webp", RotationSteps.Left270),
//     new("C:/Users/GERMANTATE/Downloads/sample-4.webp", RotationSteps.UpsideDown180),
//     new("C:/Users/GERMANTATE/Downloads/sample-3.webp"),
//     new("C:/Users/GERMANTATE/Downloads/sample-2.webp", RotationSteps.Right90),
//     new("C:/Users/GERMANTATE/Downloads/sample-1.webp"),
//     new("C:/Users/GERMANTATE/Downloads/sample-5.png", RotationSteps.UpsideDown180),
//     new("C:/Users/GERMANTATE/Downloads/sample-4.png", RotationSteps.Left270),
//     new("C:/Users/GERMANTATE/Downloads/sample-3.png"),
//     new("C:/Users/GERMANTATE/Downloads/sample-2.png", RotationSteps.Right90),
//     new("C:/Users/GERMANTATE/Downloads/sample-1.png"),
//     new("C:/Users/GERMANTATE/Downloads/sample-1.jpg", RotationSteps.UpsideDown180)


//     ];

//     string targetDirectory =
//         Path.Combine(Directory.GetCurrentDirectory(), "Output");

//     Console.WriteLine("Processing images...");

//     string savedPdfPath = ImageToPdfEngine.ConvertToPdf(
//         images: images,
//         saveDirectory: targetDirectory,
//         filename: "FinalPresentation",
//         pageSize: PageSizeOption.FitToImage,
//         orientation: OrientationOption.Auto,
//         margin: MarginOption.None,
//         imageFit: ImageFitOption.FitKeepRatio,
//         quality: 99
//     );

//     Console.WriteLine(
//         $"\n[SUCCESS] PDF exceptionally constructed and saved to:\n-> {savedPdfPath}");
// }
// catch (Exception ex)
// {
//     Console.WriteLine($"\n[FATAL ERROR]: {ex}");
// }








// // THIRD PDF TO IMAGE
// using System;
// using System.Diagnostics;
// using System.IO;
// using System.Linq;
// using PdfEngine; // Ensure this matches the namespace where PdfToImageConverter lives

// Console.ForegroundColor = ConsoleColor.Cyan;
// Console.WriteLine("=================================================");
// Console.WriteLine("   🚀 HIGH-TIER PDF TO IMAGE EXTRACTOR ENGINE");
// Console.WriteLine("=================================================\n");
// Console.ResetColor();

// // 1. Define Paths (Swap this out with a real heavy PDF on your machine)
// string testPdfPath = "C:/Users/GERMANTATE/Downloads/Untitled document (4).pdf"; 
// string baseOutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "PdfExtracts");

// try
// {
//     // --- QUICK SANITY CHECK FOR TESTING ---
//     if (!File.Exists(testPdfPath))
//     {
//         Console.ForegroundColor = ConsoleColor.Yellow;
//         Console.WriteLine($"[WARNING] Test PDF not found at: {testPdfPath}");
//         Console.WriteLine("Please change the 'testPdfPath' variable to point to a real PDF on your computer.");
//         Console.ResetColor();
//         return;
//     }



//     Console.WriteLine($"Target PDF: {Path.GetFileName(testPdfPath)}");
//     Console.WriteLine($"Output Dir: {baseOutputDirectory}");
//     Console.WriteLine("Status:     Shredding PDF to RAM and flushing to disk...\n");

//     // 2. Start Timer to track Beast Mode Speed
//     Stopwatch sw = Stopwatch.StartNew();

//     // 3. EXECUTE THE PURE ENGINE
//     // We set DPI to 150 for the sweet spot of crispness and manageable file size.
//     var savedImages = PdfToImageConverter.ConvertPdfToImages(
//         pdfPath: testPdfPath, 
//         outputPath: baseOutputDirectory, 
//         dpi: 200,
//         quality : 100
//     );

//     sw.Stop();

//     // 4. Output Results
//     Console.WriteLine("✨ --- EXTRACTION COMPLETE --- ✨\n");
    
//     Console.ForegroundColor = ConsoleColor.Green;
//     Console.WriteLine($"✅ Successfully rasterized {savedImages.Count} pages in {sw.ElapsedMilliseconds} ms!");
//     Console.WriteLine($"⏱️ Average Speed: {sw.ElapsedMilliseconds / Math.Max(1, savedImages.Count)} ms per page\n");
//     Console.ResetColor();

//     // Print the first 5 and last 5 so we don't flood the console if it's an 800 page document
//     if (savedImages.Count <= 10)
//     {
//         foreach (var path in savedImages)
//         {
//             Console.WriteLine($"  [SAVED] -> {path}");
//         }
//     }
//     else
//     {
//         foreach (var path in savedImages.Take(5))
//         {
//             Console.WriteLine($"  [SAVED] -> {path}");
//         }
//         Console.WriteLine("  ... [snip] ...");
//         foreach (var path in savedImages.Skip(savedImages.Count - 5))
//         {
//             Console.WriteLine($"  [SAVED] -> {path}");
//         }
//     }

//     // Let the user know exactly where the safe folder was generated
//     Console.ForegroundColor = ConsoleColor.Cyan;
//     Console.WriteLine($"\n📁 Final Destination Folder: {Path.GetDirectoryName(savedImages.First())}");
//     Console.ResetColor();
// }
// catch (Exception ex)
// {
//     Console.ForegroundColor = ConsoleColor.Red;
//     Console.WriteLine($"\n❌ [CRITICAL SYSTEM FAILURE]: {ex.Message}");
//     Console.WriteLine(ex.StackTrace);
//     Console.ResetColor();
// }





















// // FORUTH PDF MERGER

// using System;
// using System.Diagnostics;
// using System.IO;
// using System.Linq;
// using PdfUtilities; // Ensure this matches the namespace of your PdfMerger class

// Console.ForegroundColor = ConsoleColor.Cyan;
// Console.WriteLine("=================================================");
// Console.WriteLine("   🚀 HIGH-TIER PDF STRUCTURAL MERGER ENGINE");
// Console.WriteLine("=================================================\n");
// Console.ResetColor();

// // 1. Define Paths (Swap these out with real PDFs on your machine)
// string[] testPdfPaths = new string[] 
// {
// "C:/Users/GERMANTATE/Downloads/china.pdf",
// "C:/Users/GERMANTATE/Downloads/japan.pdf",
// "C:/Users/GERMANTATE/Downloads/ArtOfWar.pdf",
// "C:/Users/GERMANTATE/Downloads/canva1.pdf",
// "C:/Users/GERMANTATE/Downloads/canva2.pdf"
// }; 

// string baseOutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "MergedOutput");
// string desiredFileName = "Master_Merged_Document.pdf";

// try
// {
//     // --- QUICK SANITY CHECK FOR TESTING ---
//     var missingFiles = testPdfPaths.Where(path => !File.Exists(path)).ToList();
//     if (missingFiles.Any())
//     {
//         Console.ForegroundColor = ConsoleColor.Yellow;
//         Console.WriteLine("[WARNING] The following source PDFs were not found:");
//         foreach (var missing in missingFiles)
//         {
//             Console.WriteLine($" -> {missing}");
//         }
//         Console.WriteLine("\nPlease update the 'testPdfPaths' array to point to real PDFs on your computer.");
//         Console.ResetColor();
//         return;
//     }

//     Console.WriteLine($"Merging {testPdfPaths.Length} PDF files...");
//     Console.WriteLine($"Target Dir: {baseOutputDirectory}");
//     Console.WriteLine($"Target Name:{desiredFileName}");
//     Console.WriteLine("Status:     Executing binary-structural merge (Zero RAM Bloat)...\n");

//     // 2. Start Timer to track Beast Mode Speed
//     Stopwatch sw = Stopwatch.StartNew();

//     // 3. EXECUTE THE PURE ENGINE
//     string finalSavedPath = PdfMerger.Merge(
//         pdfPaths: testPdfPaths, 
//         filePathToSave: baseOutputDirectory, 
//         newFileName: desiredFileName
//     );

//     sw.Stop();

//     // 4. Output Results
//     Console.WriteLine("✨ --- MERGE COMPLETE --- ✨\n");
    
//     Console.ForegroundColor = ConsoleColor.Green;
//     Console.WriteLine($"✅ Successfully bound {testPdfPaths.Length} documents structurally in {sw.ElapsedMilliseconds} ms!");
//     Console.ResetColor();

//     // Print the inputs
//     Console.WriteLine("\n[SOURCE DOCUMENTS]:");
//     foreach (var path in testPdfPaths)
//     {
//         Console.WriteLine($"  IN  <- {Path.GetFileName(path)}");
//     }

//     // Let the user know exactly where the safe file was generated (shows if collision logic activated)
//     Console.ForegroundColor = ConsoleColor.Cyan;
//     Console.WriteLine($"\n[FINAL DESTINATION]:");
//     Console.WriteLine($"  OUT -> {finalSavedPath}");
    
//     // Check if the auto-renamer had to step in
//     if (Path.GetFileName(finalSavedPath) != desiredFileName)
//     {
//         Console.ForegroundColor = ConsoleColor.DarkGray;
//         Console.WriteLine($"  (Note: Filename was auto-adjusted to prevent overwriting an existing file)");
//     }
    
//     Console.ResetColor();
// }
// catch (Exception ex)
// {
//     Console.ForegroundColor = ConsoleColor.Red;
//     Console.WriteLine($"\n❌ [CRITICAL SYSTEM FAILURE]: {ex.Message}");
//     Console.WriteLine(ex.StackTrace);
//     Console.ResetColor();
// }



















// // FIFTH OFFICE MERGER
// using System;
// using System.Diagnostics;
// using System.IO;
// using System.Linq;
// using Orchestration; // Matches the namespace of your OfficeBatchToPdfMerger class

// Console.ForegroundColor = ConsoleColor.Cyan;
// Console.WriteLine("=================================================");
// Console.WriteLine("   🚀 HIGH-TIER OFFICE -> PDF BATCH ORCHESTRATOR");
// Console.WriteLine("=================================================\n");
// Console.ResetColor();

// // 1. Define Paths & Modes
// string[] testFilePaths = new string[] 
// {
//     @"C:\Users\GERMANTATE\Downloads\part1.docx",
//     @"C:\Users\GERMANTATE\Downloads\part1.docx"
// }; 

// // IMPORTANT: The orchestrator requires the absolute path to LibreOffice
// string libreOfficePath = @"C:\Users\GERMANTATE\Documents\LibreOfficePortable\App\libreoffice\program\soffice.exe"; 

// string baseOutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "MergedOutput");
// string desiredFileName = "Master_Merged_Document"; // Used only if mergeOption == 1

// // NEW PARAMETER: 1 to Merge all files, 0 to convert and dump individually
// int mergeOption = 1; 

// try
// {
//     // --- QUICK SANITY CHECK FOR TESTING ---
//     bool abort = false;

//     if (!File.Exists(libreOfficePath))
//     {
//         Console.ForegroundColor = ConsoleColor.Red;
//         Console.WriteLine($"[FATAL] LibreOffice executable not found at: {libreOfficePath}");
//         Console.WriteLine("Please update 'libreOfficePath' to point to your local soffice.exe installation.\n");
//         abort = true;
//     }

//     var missingFiles = testFilePaths.Where(path => !File.Exists(path)).ToList();
//     if (missingFiles.Any())
//     {
//         Console.ForegroundColor = ConsoleColor.Yellow;
//         Console.WriteLine("[WARNING] The following source files were not found:");
//         foreach (var missing in missingFiles)
//         {
//             Console.WriteLine($" -> {missing}");
//         }
//         Console.WriteLine("\nPlease update the 'testFilePaths' array to point to real files on your computer.");
//         abort = true;
//     }

//     if (abort)
//     {
//         Console.ResetColor();
//         return;
//     }

//     // Ensure output directory exists before executing
//     if (!Directory.Exists(baseOutputDirectory))
//     {
//         Directory.CreateDirectory(baseOutputDirectory);
//     }

//     Console.WriteLine($"Processing {testFilePaths.Length} files...");
//     Console.WriteLine($"Gateway:     {libreOfficePath}");
//     Console.WriteLine($"Target Dir:  {baseOutputDirectory}");
//     Console.WriteLine($"Merge Mode:  {(mergeOption == 1 ? $"ON (Target: {desiredFileName}.pdf)" : "OFF (Individual Dumps)")}");
//     Console.WriteLine("Status:      Spinning up headless conversion sandboxes...\n");

//     // 2. Start Timer to track Beast Mode Speed
//     Stopwatch sw = Stopwatch.StartNew();

//     // 3. EXECUTE THE ORCHESTRATOR (Now returns string[])
//     string[] finalSavedPaths = OfficeBatchToPdfMerger.ConvertAndMerge(
//         inputPaths: testFilePaths, 
//         newFileName: desiredFileName,
//         filePathToSave: baseOutputDirectory, 
//         libreOfficeExePath: libreOfficePath,
//         mode: "docx-pdf",
//         merge: mergeOption // Passed here
//     );

//     sw.Stop();

//     // 4. Output Results
//     Console.WriteLine("✨ --- ORCHESTRATION COMPLETE --- ✨\n");
    
//     Console.ForegroundColor = ConsoleColor.Green;
//     Console.WriteLine($"✅ Successfully processed {testFilePaths.Length} document(s) structurally in {sw.ElapsedMilliseconds} ms!");
//     Console.ResetColor();

//     // Print the inputs
//     Console.WriteLine("\n[SOURCE DOCUMENTS]:");
//     foreach (var path in testFilePaths)
//     {
//         Console.WriteLine($"  IN  <- {Path.GetFileName(path)}");
//     }

//     // Let the user know exactly where the safe files were generated
//     Console.ForegroundColor = ConsoleColor.Cyan;
//     Console.WriteLine($"\n[FINAL DESTINATION(S)]:");
//     foreach (var savedPath in finalSavedPaths)
//     {
//         Console.WriteLine($"  OUT -> {savedPath}");
        
//         // Let the user know if the collision handler renamed something
//         if (mergeOption == 1 && Path.GetFileName(savedPath) != $"{desiredFileName}.pdf")
//         {
//             Console.ForegroundColor = ConsoleColor.DarkGray;
//             Console.WriteLine($"         (Note: Filename auto-adjusted by collision handler)");
//             Console.ForegroundColor = ConsoleColor.Cyan;
//         }
//     }
    
//     Console.ResetColor();
// }
// catch (Exception ex)
// {
//     Console.ForegroundColor = ConsoleColor.Red;
//     Console.WriteLine($"\n❌ [CRITICAL SYSTEM FAILURE]: {ex.Message}");
//     Console.WriteLine(ex.StackTrace);
//     Console.WriteLine("\nNOTE: The volatile sandbox was automatically nuked to prevent system clutter.");
//     Console.ResetColor();
// }



























// SIXTH LIBRE API 
// using System;
// using System.Diagnostics;
// using System.IO;
// using System.Linq;
// using DesktopEngine.Sys; // Matches the namespace of your new CommandGateway class

// Console.ForegroundColor = ConsoleColor.Cyan;
// Console.WriteLine("=================================================");
// Console.WriteLine("    🚀 UN-KILLABLE BINARY CONVERSION ENGINE");
// Console.WriteLine("=================================================\n");
// Console.ResetColor();

// // 1. System Paths & Environment Configuration
// string libreOfficePath = @"C:\Users\GERMANTATE\Documents\lothinholder\lothin\App\libreoffice\program\soffice.exe";

// string[] testSourcePaths = new string[] 
// {
//     @"C:\Users\GERMANTATE\Downloads\partx.docx",
//     @"C:\Users\GERMANTATE\Downloads\part1.docx"
// }; 

// string baseOutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ConvertedOutput");

// try
// {
//     // --- ENGINE CRITICAL SANITY CHECKS ---
//     if (!File.Exists(libreOfficePath))
//     {
//         Console.ForegroundColor = ConsoleColor.Red;
//         Console.WriteLine($"[CRITICAL FAILURE] LibreOffice binary missing at target path:\n -> {libreOfficePath}");
//         Console.ResetColor();
//         return;
//     }

//     var missingFiles = testSourcePaths.Where(path => !File.Exists(path)).ToList();
//     if (missingFiles.Any())
//     {
//         Console.ForegroundColor = ConsoleColor.Yellow;
//         Console.WriteLine("[WARNING] The following source target files were not found:");
//         foreach (var missing in missingFiles)
//         {
//             Console.WriteLine($" -> {missing}");
//         }
//         Console.WriteLine("\nPlease verify files exist in your Downloads folder before executing.");
//         Console.ResetColor();
//         return;
//     }

//     // Ensure local destination output vault exists
//     if (!Directory.Exists(baseOutputDirectory))
//     {
//         Directory.CreateDirectory(baseOutputDirectory);
//     }

//     Console.WriteLine($"Found Core Binary: {Path.GetFileName(libreOfficePath)}");
//     Console.WriteLine($"Queued Jobs:      {testSourcePaths.Length} assets ready for compilation");
//     Console.WriteLine($"Target Directory: {baseOutputDirectory}");
//     Console.WriteLine("Execution Policy: Pure Single-Threaded Blocking. No superhero parallel garbage.\n");
//     Console.WriteLine("-------------------------------------------------\n");

//     // 2. Start Global Performance Timer
//     Stopwatch globalClock = Stopwatch.StartNew();

//     // 3. EXECUTE THE SINGLE-THREADED LOOP
//     // The engine processes exactly 1 file at a time, blocks until complete, and streams destination.
//     for (int i = 0; i < testSourcePaths.Length; i++)
//     {
//         string currentFile = testSourcePaths[i];
//         string originalFileName = Path.GetFileName(currentFile);
//         string baseNameNoExt = Path.GetFileNameWithoutExtension(currentFile);
        
//         Console.Write($"[{i + 1}/{testSourcePaths.Length}] Compiling visual layout for: {originalFileName}... ");

//         // Determine the target conversion mode based on the input extension dynamically
//         string currentExtension = Path.GetExtension(currentFile).ToLower();
//         string executionMode = currentExtension == ".pptx" ? "pptx-pdf" : "docx-pdf";
//         string targetExtension = ".pdf";

//         Stopwatch itemClock = Stopwatch.StartNew();

//         // Fire the single-threaded agnostic gateway
//         string finalSavedPath = CommandGateway.Convert(
//             libreOfficeExePath: libreOfficePath,
//             filepath: currentFile,
//             newFilename: baseNameNoExt, // Keep original base name as default target
//             folderPath: baseOutputDirectory,
//             mode: executionMode
//         );

//         itemClock.Stop();

//         Console.ForegroundColor = ConsoleColor.Green;
//         Console.WriteLine($"DONE ({itemClock.ElapsedMilliseconds} ms)");
//         Console.ResetColor();

//         // Print final location details and flag if the collision engine handled a file name overwrite
//         Console.WriteLine($"  └─ OUT -> {finalSavedPath}");
        
//         string expectedDefaultName = $"{baseNameNoExt}{targetExtension}";
//         if (Path.GetFileName(finalSavedPath) != expectedDefaultName)
//         {
//             Console.ForegroundColor = ConsoleColor.DarkGray;
//             Console.WriteLine($"  └─ [SYSTEM NOTE]: File collision detected. Safely re-indexed to prevent data corruption.");
//             Console.ResetColor();
//         }
//         Console.WriteLine();
//     }

//     globalClock.Stop();

//     // 4. Final System Summary Output
//     Console.WriteLine("✨ --- ALL PIPELINE JOBS COMPLETE --- ✨\n");
    
//     Console.ForegroundColor = ConsoleColor.Green;
//     Console.WriteLine($"✅ Successfully compiled and verified {testSourcePaths.Length} documents via headless vector translation!");
//     Console.ForegroundColor = ConsoleColor.Cyan;
//     Console.WriteLine($"🚀 Total Pipeline Running Time: {globalClock.ElapsedMilliseconds} ms");
//     Console.ResetColor();
// }
// catch (Exception ex)
// {
//     Console.ForegroundColor = ConsoleColor.Red;
//     Console.WriteLine($"\n❌ [CRITICAL ENGINE PIPELINE FAILURE]: {ex.Message}");
//     Console.WriteLine(ex.StackTrace);
//     Console.ResetColor();
// }
























// // SEVENTH UNZIPPER
// using System;
// using System.Diagnostics;
// using System.IO;
// using System.IO.Compression;

// Console.ForegroundColor = ConsoleColor.Cyan;
// Console.WriteLine("=================================================");
// Console.WriteLine("    🚀 NATIVE .NET ZIP EXTRACTION BENCHMARK     ");
// Console.WriteLine("=================================================\n");
// Console.ResetColor();

// string sourceZip = @"C:\Users\GERMANTATE\Documents\lothin.zip";
// string targetDir = @"C:\Users\GERMANTATE\Documents\lothinholder";

// try
// {
//     // 1. Guardrail Check
//     if (!File.Exists(sourceZip))
//     {
//         Console.ForegroundColor = ConsoleColor.Red;
//         Console.WriteLine($"[FATAL ERROR] Source archive missing at: {sourceZip}");
//         Console.ResetColor();
//         return;
//     }

//     // 2. Clear out target directory for a 100% clean benchmark
//     if (Directory.Exists(targetDir))
//     {
//         Console.ForegroundColor = ConsoleColor.Yellow;
//         Console.WriteLine("🔄 Target folder detected. Purging old directory layout for an accurate benchmark...");
//         Console.ResetColor();
//         Directory.Delete(targetDir, true);
//     }
    
//     Directory.CreateDirectory(targetDir);

//     Console.WriteLine($"📦 Source Payload: {Path.GetFileName(sourceZip)}");
//     Console.WriteLine($"📁 Destination:    {targetDir}");
//     Console.WriteLine("⏳ Status:         Decompressing bitstream natively via hardware-accelerated Deflate...");
//     Console.WriteLine("-------------------------------------------------\n");

//     // 3. Start High-Precision Timer and Execute
//     Stopwatch sw = Stopwatch.StartNew();
    
//     ZipFile.ExtractToDirectory(sourceZip, targetDir);
    
//     sw.Stop();

//     // 4. Output Performance Metrics
//     Console.WriteLine("✨ --- DECOMPRESSION PIPELINE COMPLETE --- ✨\n");
    
//     Console.ForegroundColor = ConsoleColor.Green;
//     Console.WriteLine("✅ Successfully unpacked Lothin binaries layout to disk with zero external dependencies!");
//     Console.ForegroundColor = ConsoleColor.Cyan;
//     Console.WriteLine($"🚀 Total Running Time: {sw.ElapsedMilliseconds} ms ({sw.Elapsed.TotalSeconds:F2} seconds)");
//     Console.ResetColor();
// }
// catch (Exception ex)
// {
//     Console.ForegroundColor = ConsoleColor.Red;
//     Console.WriteLine($"\n❌ [CRITICAL PIPELINE FAILURE]: {ex.Message}");
//     Console.WriteLine(ex.StackTrace);
//     Console.ResetColor();
// }












































// // cat sim
// using Avalonia;
// using Avalonia.Controls;
// using Avalonia.Layout;
// using Avalonia.Media;
// using Avalonia.Themes.Fluent;
// using Avalonia.Threading;
// using System;
// using System.Threading.Tasks;

// // Resolve the name collision between System.IO.Path and Avalonia.Controls.Shapes.Path
// using Path = Avalonia.Controls.Shapes.Path;

// namespace InteractiveCatApp;

// class Program
// {
//     [STAThread]
//     public static void Main(string[] args) => BuildAvaloniaApp()
//         .StartWithClassicDesktopLifetime(args);

//     public static AppBuilder BuildAvaloniaApp() =>
//         AppBuilder.Configure<App>()
//             .UsePlatformDetect()
//             .LogToTrace();
// }

// public class App : Application
// {
//     public override void Initialize() => Styles.Add(new FluentTheme());

//     public override void OnFrameworkInitializationCompleted()
//     {
//         if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
//         {
//             desktop.MainWindow = new MainWindow();
//         }
//         base.OnFrameworkInitializationCompleted();
//     }
// }

// public class MainWindow : Window
// {
//     private Path eye1, eye2, eye3, eye4;
//     private Button btnBlip, btnRest, btnFurious;
//     private Canvas eyesContainer;
//     private DispatcherTimer blinkTimer;

//     public MainWindow()
//     {
//         Title = "Interactive Cat SVG Test";
//         Width = 600;
//         Height = 500;
//         Background = SolidColorBrush.Parse("#1a1a1a");
//         WindowStartupLocation = WindowStartupLocation.CenterScreen;

//         // Container Stack Setup
//         var mainStack = new StackPanel
//         {
//             VerticalAlignment = VerticalAlignment.Center,
//             HorizontalAlignment = HorizontalAlignment.Center,
//             Spacing = 20
//         };

//         // Viewbox behaves exactly like SVG viewBox scaling
//         var viewbox = new Viewbox
//         {
//             MaxWidth = 500,
//             Stretch = Stretch.Uniform
//         };

//         // Main Black Canvas base (Matching 75.5 x 44.3 coordinate space)
//         var canvas = new Canvas
//         {
//             Width = 75.52,
//             Height = 44.32,
//             Background = Brushes.Black
//         };

//         // Factory function for handling SVG path definitions
//         Path CreateCatPath(string data, double x, double y)
//         {
//             var path = new Path
//             {
//                 Data = Geometry.Parse(data),
//                 Stroke = Brushes.White,
//                 StrokeThickness = 1.5,
//                 StrokeLineCap = PenLineCap.Round,
//                 Fill = Brushes.Transparent
//             };
//             Canvas.SetLeft(path, x);
//             Canvas.SetTop(path, y);
//             return path;
//         }

//         // Add Base Structural Cat Paths (Ears, Face, Mouth)
//         canvas.Children.Add(CreateCatPath("M0 0 C2.66 -7.87, 5.31 -15.74, 7.59 -22.49 M0 0 C1.96 -5.82, 3.93 -11.64, 7.59 -22.49", 10, 32.49));
//         canvas.Children.Add(CreateCatPath("M0 0 C3.01 2.99, 6.02 5.97, 9.89 9.81 M0 0 C2.91 2.89, 5.82 5.78, 9.89 9.81", 17.86, 10.01));
//         canvas.Children.Add(CreateCatPath("M0 0 C4.14 0.03, 8.29 0.06, 18.55 0.13 M0 0 C6.74 0.05, 13.47 0.09, 18.55 0.13", 27.91, 19.88));
//         canvas.Children.Add(CreateCatPath("M0 0 C3.51 -3.15, 7.03 -6.29, 10.87 -9.73 M0 0 C4.09 -3.66, 8.18 -7.32, 10.87 -9.73", 46.43, 20.01));
//         canvas.Children.Add(CreateCatPath("M0 0 C-2.62 -7.16, -5.23 -14.31, -8.18 -22.38 M0 0 C-2.3 -6.3, -4.61 -12.6, -8.18 -22.38", 65.52, 32.57));
//         canvas.Children.Add(CreateCatPath("M0 0 C2.92 0.01, 5.84 0.02, 10.73 0.04 M0 0 C4.01 0.01, 8.03 0.03, 10.73 0.04", 32.26, 34.29));

//         // Group Container Canvas for Eyes (Handles blinking scaling)
//         eyesContainer = new Canvas
//         {
//             Width = 75.52,
//             Height = 44.32,
//             RenderTransformOrigin = new RelativePoint(38, 27, RelativeUnit.Absolute),
//             RenderTransform = new ScaleTransform()
//         };

//         // Initialize Dynamic Eye Segments
//         eye1 = CreateCatPath("M0 0 C3.35 1.69, 6.71 3.39, 9.04 4.56 M0 0 C2.1 1.06, 4.21 2.12, 9.04 4.56", 19.44, 24.26);
//         eye2 = CreateCatPath("M0 0 C-3.39 0.02, -6.78 0.05, -10.11 0.07 M0 0 C-2.48 0.02, -4.96 0.04, -10.11 0.07", 28.42, 29.51);
//         eye3 = CreateCatPath("M0 0 C3.45 -1.34, 6.9 -2.68, 9.31 -3.62 M0 0 C3.35 -1.3, 6.7 -2.6, 9.31 -3.62", 46.62, 28.47);
//         eye4 = CreateCatPath("M0 0 C-2.74 -0.01, -5.47 -0.02, -10.51 -0.03 M0 0 C-3.81 -0.01, -7.62 -0.03, -10.51 -0.03", 57.15, 29.32);

//         eyesContainer.Children.Add(eye1);
//         eyesContainer.Children.Add(eye2);
//         eyesContainer.Children.Add(eye3);
//         eyesContainer.Children.Add(eye4);

//         canvas.Children.Add(eyesContainer);
//         viewbox.Child = canvas;
//         mainStack.Children.Add(viewbox);

//         // Control Panel Stack Setup
//         var controlsStack = new StackPanel
//         {
//             Orientation = Orientation.Horizontal,
//             Spacing = 15,
//             HorizontalAlignment = HorizontalAlignment.Center
//         };

//         Button CreateButton(string text)
//         {
//             return new Button
//             {
//                 Content = text,
//                 Padding = new Thickness(24, 12),
//                 FontSize = 16,
//                 CornerRadius = new CornerRadius(6),
//                 BorderThickness = new Thickness(2)
//             };
//         }

//         btnBlip = CreateButton("Blip");
//         btnRest = CreateButton("Rest");
//         btnFurious = CreateButton("Furious");

//         btnBlip.Click += (s, e) => SetEyeState("blip");
//         btnRest.Click += (s, e) => SetEyeState("rest");
//         btnFurious.Click += (s, e) => SetEyeState("furious");

//         controlsStack.Children.Add(btnBlip);
//         controlsStack.Children.Add(btnRest);
//         controlsStack.Children.Add(btnFurious);

//         mainStack.Children.Add(controlsStack);
//         Content = mainStack;

//         // Setup Blinking dispatcher loop (Matches 4s CSS block)
//         blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
//         blinkTimer.Tick += BlinkTimer_Tick;

//         // Fire Default State
//         SetEyeState("blip");
//     }

//     private void SetEyeState(string state)
//     {
//         // Toggle individual path structures based on configuration rules:
//         // blip = all elements, rest = paths 2 and 4, furious = paths 1 and 3
//         eye1.IsVisible = (state == "blip" || state == "furious");
//         eye2.IsVisible = (state == "blip" || state == "rest");
//         eye3.IsVisible = (state == "blip" || state == "furious");
//         eye4.IsVisible = (state == "blip" || state == "rest");

//         // Handle animation activation state
//         if (state == "blip")
//         {
//             blinkTimer.Start();
//         }
//         else
//         {
//             blinkTimer.Stop();
//             ResetBlinkScale();
//         }

//         // Refresh dynamic UI Active Element Styles
//         StyleButton(btnBlip, state == "blip");
//         StyleButton(btnRest, state == "rest");
//         StyleButton(btnFurious, state == "furious");
//     }

//     private void StyleButton(Button btn, bool isActive)
//     {
//         if (isActive)
//         {
//             btn.Background = Brushes.White;
//             btn.Foreground = SolidColorBrush.Parse("#1a1a1a");
//             btn.BorderBrush = Brushes.White;
//             btn.FontWeight = FontWeight.Bold;
//         }
//         else
//         {
//             btn.Background = SolidColorBrush.Parse("#333");
//             btn.Foreground = Brushes.White;
//             btn.BorderBrush = SolidColorBrush.Parse("#ffffff33");
//             btn.FontWeight = FontWeight.Normal;
//         }
//     }

//     // Handled warning by explicitly marking sender argument as nullable (object? sender)
//     private async void BlinkTimer_Tick(object? sender, EventArgs e)
//     {
//         if (eyesContainer.RenderTransform is ScaleTransform scale)
//         {
//             // Fast programmatic snap down and up to simulate a quick blink
//             scale.ScaleY = 0.1;
//             await Task.Delay(100);
//             scale.ScaleY = 1.0;
//         }
//     }

//     private void ResetBlinkScale()
//     {
//         if (eyesContainer.RenderTransform is ScaleTransform scale)
//         {
//             scale.ScaleY = 1.0;
//         }
//     }
// }