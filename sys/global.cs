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
        public static bool dataReload = false;

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