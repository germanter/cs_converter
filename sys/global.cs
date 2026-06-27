using System;

namespace Glo
{
    /// <summary>
    /// Application-wide global configuration registry.
    /// Modifiable and readable from any script across the assembly.
    /// </summary>
    public static class Vars
    {
        // Safe empty string initialization. No null-handling madness.
        public static bool initLock = false;
        public static string libreDIR = @"";
        public static string dataDIR = @"";

        public static string mainDIR = @"";
        public static bool openLog = true;

        // Add this right underneath it:
        // --- NEW: THE BLACK MAGIC EVENT SYSTEM ---
private static string _jsonSnapshot = @"";

        // Anyone (like the UI) can subscribe to this event to listen for changes
        public static event Action<string>? OnSnapshotChanged;

        public static string jsonSnapshot
        {
            get => _jsonSnapshot;
            set
            {
                if (_jsonSnapshot != value)
                {
                    _jsonSnapshot = value;
                    // Fire the alarm! Pass the new json to whoever is listening.
                    OnSnapshotChanged?.Invoke(_jsonSnapshot);
                }
            }
        }

        public static event Action? OnThemeChanged; // NEW: The UI color/theme broadcast engine
        public static void NotifyThemeChanged() => OnThemeChanged?.Invoke();
        public static string baseBGcolor = "#1E1E1E"; // core
        public static string baseTEXTcolor = "#FFFFFF"; // core

        public static string BGcolor = "#1E1E1E";
        public static string TEXTcolor = "#FFFFFF";

        public static readonly Dictionary<string, string> ctgMap = new()
        {
            { "Image2PDF", "image2pdf" },
            { "ImageConverter", "imageconverter" },
            { "Office2PDF", "office2pdf" },
            { "PDF2Image", "pdf2image" },
            { "PDFMerger", "pdfmerger" }
        };
    }
}