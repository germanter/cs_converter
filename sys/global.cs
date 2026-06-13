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

    }
}