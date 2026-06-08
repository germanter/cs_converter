// using System;  //// FIRST IMAGE TO IMAGE
// using System.IO;
// using System.Threading;
// using System.Threading.Tasks;

// // 1. Validate Command Line Arguments
// if (args.Length < 3)
// {
//     Console.ForegroundColor = ConsoleColor.Yellow;
//     Console.WriteLine("⚠️ Missing Arguments!");
//     Console.WriteLine("Usage:   dotnet run -- <format> <output_directory> <image1> <image2> ...");
//     Console.WriteLine("Example: dotnet run -- webp ./ConvertedImages ./myimage.png ./myphoto.jpg");
//     Console.ResetColor();
//     return;
// }

// string targetFormat = args[0];
// string outputPath = args[1];

// // Extract all trailing image paths from argument index 2 through the end
// string[] sourceImages = args[2..];

// Console.WriteLine($"🚀 Starting conversion of {sourceImages.Length} image(s) to '{targetFormat.ToUpperInvariant()}'...\n");

// // 2. Wire Up the Progress Reporter (Leveraging your engine's IProgress implementation)
// var progressReporter = new Progress<double>(percent =>
// {
//     // The "\r" characters forces the console cursor back to the start of the line 
//     // to provide a smooth, in-place percentage counter.
//     Console.Write($"\r🔄 Progress: [{percent:F1}%] Processing assets...");
// });

// // 3. Graceful Cancellation Handling (Tied directly to your engine's CancellationToken)
// using var cts = new CancellationTokenSource();
// Console.CancelKeyPress += (sender, e) =>
// {
//     Console.ForegroundColor = ConsoleColor.Yellow;
//     Console.WriteLine("\n🛑 Cancellation requested! Halting execution safely...");
//     Console.ResetColor();
    
//     cts.Cancel();
//     e.Cancel = true; // Prevents the operating system from abruptly killing the process
// };

// try
// {
//     // 4. Execute the Beast Mode Async Image Converter
//     var convertedFiles = await UltimateImageConverter.ConvertImagesAsync(
//         sourceImages, 
//         targetFormat, 
//         outputPath, 
//         progressReporter, 
//         cts.Token
//     );

//     // 5. Output Processing Results
//     Console.WriteLine("\n\n✨ --- CONVERSION COMPLETE --- ✨");
    
//     if (convertedFiles.Count == sourceImages.Length)
//     {
//         Console.ForegroundColor = ConsoleColor.Green;
//         Console.WriteLine($"✅ All images successfully processed: {convertedFiles.Count}/{sourceImages.Length}\n");
//     }
//     else
//     {
//         Console.ForegroundColor = ConsoleColor.Yellow;
//         Console.WriteLine($"⚠️ Completed with skipped files or errors: {convertedFiles.Count}/{sourceImages.Length}\n");
//     }
//     Console.ResetColor();

//     foreach (var path in convertedFiles)
//     {
//         Console.WriteLine($"  [DONE] -> {path}");
//     }
// }
// catch (OperationCanceledException)
// {
//     Console.ForegroundColor = ConsoleColor.Red;
//     Console.WriteLine("\n❌ Operation aborted by the user.");
//     Console.ResetColor();
// }
// catch (Exception ex)
// {
//     Console.ForegroundColor = ConsoleColor.Red;
//     Console.WriteLine($"\n❌ Critical System Failure: {ex.Message}");
//     Console.ResetColor();
// }


/* ===================================================================================
💡 NOTE FOR FUTURE AVALONIA DESKTOP GUI TRANSITION:
When you are ready to switch from this CLI test tool to a full Avalonia Desktop App,
comment out everything above this box, and uncomment the setup sequence below.
===================================================================================

using Avalonia;

class Program
{
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<ui.App>()
            .UsePlatformDetect()
            .LogToTrace();
}
*/






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





















// FORUTH PDF MERGER

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PdfUtilities; // Ensure this matches the namespace of your PdfMerger class

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("=================================================");
Console.WriteLine("   🚀 HIGH-TIER PDF STRUCTURAL MERGER ENGINE");
Console.WriteLine("=================================================\n");
Console.ResetColor();

// 1. Define Paths (Swap these out with real PDFs on your machine)
string[] testPdfPaths = new string[] 
{
"C:/Users/GERMANTATE/Downloads/china.pdf",
"C:/Users/GERMANTATE/Downloads/japan.pdf",
"C:/Users/GERMANTATE/Downloads/ArtOfWar.pdf",
"C:/Users/GERMANTATE/Downloads/canva1.pdf",
"C:/Users/GERMANTATE/Downloads/canva2.pdf"
}; 

string baseOutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "MergedOutput");
string desiredFileName = "Master_Merged_Document.pdf";

try
{
    // --- QUICK SANITY CHECK FOR TESTING ---
    var missingFiles = testPdfPaths.Where(path => !File.Exists(path)).ToList();
    if (missingFiles.Any())
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("[WARNING] The following source PDFs were not found:");
        foreach (var missing in missingFiles)
        {
            Console.WriteLine($" -> {missing}");
        }
        Console.WriteLine("\nPlease update the 'testPdfPaths' array to point to real PDFs on your computer.");
        Console.ResetColor();
        return;
    }

    Console.WriteLine($"Merging {testPdfPaths.Length} PDF files...");
    Console.WriteLine($"Target Dir: {baseOutputDirectory}");
    Console.WriteLine($"Target Name:{desiredFileName}");
    Console.WriteLine("Status:     Executing binary-structural merge (Zero RAM Bloat)...\n");

    // 2. Start Timer to track Beast Mode Speed
    Stopwatch sw = Stopwatch.StartNew();

    // 3. EXECUTE THE PURE ENGINE
    string finalSavedPath = PdfMerger.Merge(
        pdfPaths: testPdfPaths, 
        filePathToSave: baseOutputDirectory, 
        newFileName: desiredFileName
    );

    sw.Stop();

    // 4. Output Results
    Console.WriteLine("✨ --- MERGE COMPLETE --- ✨\n");
    
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"✅ Successfully bound {testPdfPaths.Length} documents structurally in {sw.ElapsedMilliseconds} ms!");
    Console.ResetColor();

    // Print the inputs
    Console.WriteLine("\n[SOURCE DOCUMENTS]:");
    foreach (var path in testPdfPaths)
    {
        Console.WriteLine($"  IN  <- {Path.GetFileName(path)}");
    }

    // Let the user know exactly where the safe file was generated (shows if collision logic activated)
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"\n[FINAL DESTINATION]:");
    Console.WriteLine($"  OUT -> {finalSavedPath}");
    
    // Check if the auto-renamer had to step in
    if (Path.GetFileName(finalSavedPath) != desiredFileName)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  (Note: Filename was auto-adjusted to prevent overwriting an existing file)");
    }
    
    Console.ResetColor();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n❌ [CRITICAL SYSTEM FAILURE]: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Console.ResetColor();
}