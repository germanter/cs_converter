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
        public static string libreDIR = @"C:\Users\GERMANTATE\Documents\LibreOfficePortable\App\libreoffice\program\soffice.exe";
        public static string dataDIR = @"C:\Users\GERMANTATE\Desktop\sideProjects\cs_convx\dat\data.json";
        public static bool openLog = true;

        // Add this right underneath it:
        // --- NEW: THE BLACK MAGIC EVENT SYSTEM ---
private static string _jsonSnapshot = @"{
  ""sys"": {
    ""openLog"": true,
    ""bg"": ""#1E1E1E"",
    ""text"": ""#FFFFFF""
  },
  ""logs"": [
    {
      ""uuid"": ""ca158b75"",
      ""type"": ""office2pdf"",
      ""fullpath"": ""C:\\Users\\GERMANTATE\\Downloads\\jjj.pdf"",
      ""status"": ""success"",
      ""timestamp"": ""2026-06-15 13:31:13380""
    }
  ]
}";

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
        // ------------------------------------------


        public static event Action? OnThemeChanged; // NEW: The UI color/theme broadcast engine
        public static void NotifyThemeChanged() => OnThemeChanged?.Invoke();
        public static string baseBGcolor = "#1E1E1E";
        public static string baseTEXTcolor = "#FFFFFF";

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