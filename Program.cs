// using System;
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


// /* ===================================================================================
// 💡 NOTE FOR FUTURE AVALONIA DESKTOP GUI TRANSITION:
// When you are ready to switch from this CLI test tool to a full Avalonia Desktop App,
// comment out everything above this box, and uncomment the setup sequence below.
// ===================================================================================

// using Avalonia;

// class Program
// {
//     public static void Main(string[] args) =>
//         BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

//     public static AppBuilder BuildAvaloniaApp() =>
//         AppBuilder.Configure<ui.App>()
//             .UsePlatformDetect()
//             .LogToTrace();
// }
// */

using ImageToPdfApp;

Console.WriteLine("=== High-Tier Image to PDF Converter ===");

try
{
    List<ImageInput> images =
    [
        new("c:/Users/GERMANTATE/Downloads/ChatGPT Image May 23, 2026, 01_42_44 PM.png"),
        new("2.png"),

        // Examples:
        new("c:/Users/GERMANTATE/Downloads/ChatGPT Image May 23, 2026, 01_42_44 PM.png",RotationSteps.UpsideDown180),
        new("C:/Users/GERMANTATE/Downloads/image (1).ico", RotationSteps.Left270),
        new("C:/Users/GERMANTATE/Downloads/image (1).ico", RotationSteps.UpsideDown180),
    ];

    string targetDirectory =
        Path.Combine(Directory.GetCurrentDirectory(), "Output");

    Console.WriteLine("Processing images...");

    string savedPdfPath = ImageToPdfEngine.ConvertToPdf(
        images: images,
        saveDirectory: targetDirectory,
        filename: "FinalPresentation",
        pageSize: PageSizeOption.FitToImage,
        orientation: OrientationOption.Auto,
        margin: MarginOption.None,
        imageFit: ImageFitOption.FitKeepRatio,
        quality: 100
    );

    Console.WriteLine(
        $"\n[SUCCESS] PDF exceptionally constructed and saved to:\n-> {savedPdfPath}");
}
catch (Exception ex)
{
    Console.WriteLine($"\n[FATAL ERROR]: {ex}");
}