using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using Glo;
using WriterHead;

namespace Initialization
{
    public static class AppInitializer
    {
        private const string SignaturePrefix = "4A1F9B2C";

        /// <summary>
        /// Orchestrates pre-flight workspace verification, path configuration, and database structure checks.
        /// Unlocks the application upon success, or terminates the process safely on fatal failures.
        /// </summary>
        public static void Initialize()
        {
            string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string selectedFolderPath = "";
            bool success = false;

            // 1. Search Loop: Find or create a safe, owned convix_files workspace folder
            for (int count = 0; count < 1000; count++)
            {
                string folderName = count == 0 ? "convix_files" : $"convix_files_{count}";
                string folderPath = Path.Combine(userHome, folderName);
                string signatureName = $".{SignaturePrefix}_{folderName}";
                string signaturePath = Path.Combine(folderPath, signatureName);

                if (!Directory.Exists(folderPath))
                {
                    // Target directory does not exist, attempt to claim it
                    try
                    {
                        Directory.CreateDirectory(folderPath);
                        
                        // Drop the empty, non-disk-taxing signature file
                        File.WriteAllText(signaturePath, "");
                        
                        // Initialize workspace assets (Unzip binaries & default JSON)
                        InitializeFiles(folderPath);

                        selectedFolderPath = folderPath;
                        success = true;
                        break;
                    }
                    catch
                    {
                        // Suppress OS/Permission errors, bypass, and try the next sequential index
                        continue;
                    }
                }
                else
                {
                    // Target directory exists, test reading privileges
                    try
                    {
                        _ = Directory.GetFiles(folderPath);
                    }
                    catch
                    {
                        // Access denied, skip index to avoid breaking user permission tree
                        continue;
                    }

                    // Check if signature file exists to confirm app ownership
                    if (File.Exists(signaturePath))
                    {
                        string dataPath = Path.Combine(folderPath, "data.json");
                        string lothinPath = Path.Combine(folderPath, "lothin");
                        string librePath = Path.Combine(lothinPath, "App", "libreoffice", "program", "soffice.exe");

                        // Validate integrity of files inside the claimed workspace
                        if (!File.Exists(dataPath) || !Directory.Exists(lothinPath) || !File.Exists(librePath))
                        {
                            try
                            {
                                // Corruption/drift detected. Clear contents safely and reinstall.
                                ClearFolderExceptSignature(folderPath, signatureName);
                                InitializeFiles(folderPath);
                            }
                            catch
                            {
                                // If locked or unwriteable, leave the folder alone and seek next available index
                                continue;
                            }
                        }

                        selectedFolderPath = folderPath;
                        success = true;
                        break;
                    }
                    else
                    {
                        // Folder exists but does not belong to us (Signature missing). Move to next index.
                        continue;
                    }
                }
            }

            // Fatal: No writeable folder found within 1000 sequential indexes
            if (!success)
            {
                Environment.Exit(1);
            }

            // 2. Set Assembly paths in global variables
            Vars.mainDIR = selectedFolderPath;
            Vars.dataDIR = Path.Combine(selectedFolderPath, "data.json");
            Vars.libreDIR = Path.Combine(selectedFolderPath, "lothin", "App", "libreoffice", "program", "soffice.exe");

            // 3. Fire database structure verification checks
            var initResult = Writer.Initializer();

            if (!initResult.status)
            {
                // Recreate and re-attempt to configure default json snapshot
                try
                {
                    if (File.Exists(Vars.dataDIR))
                    {
                        File.Delete(Vars.dataDIR);
                    }
                    File.WriteAllText(Vars.dataDIR, Writer.DefaultFullJson);
                }
                catch
                {
                    // Immediate shutdown on write failures
                    Environment.Exit(1);
                }

                // Final verification run
                initResult = Writer.Initializer();
                if (!initResult.status)
                {
                    // Structural failure, close the app
                    Environment.Exit(1);
                }
            }

            // 4. Propagate workspace parameters across assembly and open main launch lock
            try
            {
                var sysNode = JsonNode.Parse(initResult.content);
                if (sysNode != null)
                {
                    if (sysNode["openLog"] != null)
                    {
                        Vars.openLog = sysNode["openLog"].GetValueKind() == JsonValueKind.True;
                    }

                    if (sysNode["bg"] != null)
                    {
                        Vars.BGcolor = sysNode["bg"].ToString();
                    }

                    if (sysNode["text"] != null)
                    {
                        Vars.TEXTcolor = sysNode["text"].ToString();
                    }
                }

                // Release global initialization lock
                Vars.initLock = true;
            }
            catch
            {
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Installs the headless LibreOffice binaries layout from base/lothin.zip and creates default full JSON.
        /// </summary>
        private static void InitializeFiles(string folderPath)
        {
            var assembly = typeof(AppInitializer).Assembly;
            
            // Default manifest resource name format: {AssemblyName}.{Folder}.{FileName}
            string resourceName = "convix.base.lothin.zip";

            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                // Dynamic fallback check in case assembly namespaces vary at compile-time
                if (stream == null)
                {
                    string? foundName = System.Linq.Enumerable.FirstOrDefault(
                        assembly.GetManifestResourceNames(),
                        n => n.EndsWith("lothin.zip", StringComparison.OrdinalIgnoreCase)
                    );

                    if (foundName != null)
                    {
                        using (Stream? fallbackStream = assembly.GetManifestResourceStream(foundName))
                        {
                            if (fallbackStream != null)
                            {
                                using (var archive = new ZipArchive(fallbackStream))
                                {
                                    archive.ExtractToDirectory(folderPath);
                                }
                                goto WriteJson;
                            }
                        }
                    }

                    throw new FileNotFoundException("System Error: Embedded binary package 'lothin.zip' was not found in the executable resource manifest.");
                }

                // Extract Zip archive directly from memory stream on the assembly block
                using (var archive = new ZipArchive(stream))
                {
                    archive.ExtractToDirectory(folderPath);
                }
            }

        WriteJson:
            // Install default json configuration
            string dataPath = Path.Combine(folderPath, "data.json");
            File.WriteAllText(dataPath, Writer.DefaultFullJson);
        }

        /// <summary>
        /// Clears all files and folders inside workspace except for the hidden signature identifier file.
        /// </summary>
        private static void ClearFolderExceptSignature(string folderPath, string signatureName)
        {
            foreach (var file in Directory.GetFiles(folderPath))
            {
                if (Path.GetFileName(file) != signatureName)
                {
                    File.Delete(file);
                }
            }

            foreach (var dir in Directory.GetDirectories(folderPath))
            {
                Directory.Delete(dir, true);
            }
        }
    }
}