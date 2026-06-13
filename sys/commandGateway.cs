// version 2 deadlock stop 
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace DesktopEngine.Sys
{
    public static class CommandGateway
    {
        /// <summary>
        /// Agnostic, single-threaded execution engine. 0% async. 0% hardcoded paths.
        /// Actively monitors process vitals and asynchronously drains streams to eliminate deadlocks.
        /// </summary>
        public static string Convert(string libreOfficeExePath, string filepath, string newFilename, string folderPath, string mode)
        {
            // =================================================================
            // ENGINE CONFIGURATION ZONE
            // =================================================================
            int maxCpuFlatlineSeconds = 12; // 12s is the gold standard for heavy files vs ghost locks.
            // =================================================================

            // 1. Guardrails: Verify structural dependencies before drawing power
            if (!File.Exists(libreOfficeExePath))
                throw new FileNotFoundException($"System Error: LibreOffice executable not found at: {libreOfficeExePath}");

            if (!File.Exists(filepath))
                throw new FileNotFoundException($"System Error: Input document source not found at: {filepath}");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // 2. Map the mode to the exact LibreOffice command and target extension
            string targetExtension = "";
            string convertArgs = "";

            switch (mode.ToLower())
            {
                case "docx-pdf":
                case "pptx-pdf":
                    targetExtension = ".pdf";
                    convertArgs = "pdf";
                    break;
                default:
                    throw new ArgumentException($"System Error: Unrecognized conversion mode '{mode}'");
            }

            // Clean the requested filename of any existing extensions to prevent "name.pdf.pdf"
            string baseName = Path.GetFileNameWithoutExtension(newFilename);
            string finalTargetPath = Path.Combine(folderPath, $"{baseName}{targetExtension}");

            // 3. The File Collision Subsystem (Silent Rename)
            int counter = 1;
            while (File.Exists(finalTargetPath))
            {
                finalTargetPath = Path.Combine(folderPath, $"{baseName} ({counter}){targetExtension}");
                counter++;
            }

            // 4. Create an isolated sandbox directory
            string sandboxDir = Path.Combine(Path.GetTempPath(), $"Engine_Sandbox_{Guid.NewGuid():N}");
            Directory.CreateDirectory(sandboxDir);

            try
            {
                // 5. Build the silent, headless execution profile
                var startInfo = new ProcessStartInfo
                {
                    FileName = libreOfficeExePath,
                    Arguments = $"--headless --invisible --norestore --nofirststartwizard --convert-to {convertArgs} --outdir \"{sandboxDir}\" \"{filepath}\"",
                    UseShellExecute = false, 
                    CreateNoWindow = true,   
                    RedirectStandardOutput = true,
                    RedirectStandardError = true // This requires active draining to prevent pipe deadlocks
                };

                // 6. Execute, Drain Buffers, and Monitor Heartbeat
                using (var process = new Process())
                {
                    process.StartInfo = startInfo;

                    // Asynchronously drain the Error and Output buffers to prevent the OS from halting LibreOffice
                    StringBuilder errorOutput = new StringBuilder();
                    process.ErrorDataReceived += (sender, args) => 
                    {
                        if (args.Data != null) errorOutput.AppendLine(args.Data);
                    };

                    // Even if we don't save the standard output, we MUST drain it to keep the pipe clear
                    process.OutputDataReceived += (sender, args) => { };

                    process.Start();

                    // Tell the process to start dumping its streams into our background handlers
                    process.BeginErrorReadLine();
                    process.BeginOutputReadLine();

                    TimeSpan lastCpuTime = TimeSpan.Zero;
                    int flatlineSeconds = 0;

                    // Interrogate the operating system kernel once every second
                    while (!process.HasExited)
                    {
                        try
                        {
                            process.Refresh(); // Force Windows to refresh the process resource tree
                            TimeSpan currentCpuTime = process.TotalProcessorTime;

                            if (currentCpuTime == lastCpuTime)
                            {
                                // The process is consuming 0.0% CPU cycles. It is completely stalled.
                                flatlineSeconds++;
                            }
                            else
                            {
                                // Math is happening! Layout engine is alive. Reset the counter.
                                flatlineSeconds = 0;
                                lastCpuTime = currentCpuTime;
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            // Process dropped across the boundary cleanly right during the check.
                            break;
                        }

                        // The Hammer: Triggered when flatline meets or exceeds your configuration parameter
                        if (flatlineSeconds >= maxCpuFlatlineSeconds)
                        {
                            process.Kill();
                            throw new TimeoutException($"Engine Execution Aborted: Process flatlined at 0% CPU usage for {maxCpuFlatlineSeconds} continuous seconds. Target asset is highly likely password-protected or heavily corrupted.");
                        }

                        Thread.Sleep(1000); // Block the execution thread for 1000ms before checking the heartbeat pulse again
                    }

                    // Process exited on its own, now double check its final reporting state
                    if (process.ExitCode != 0)
                    {
                        throw new InvalidOperationException($"Engine failed to convert. Process exited with error code {process.ExitCode}. Log: {errorOutput.ToString()}");
                    }
                }

                // 7. Locate the generated file inside the sandbox
                string originalBaseName = Path.GetFileNameWithoutExtension(filepath);
                string sandboxFile = Path.Combine(sandboxDir, $"{originalBaseName}{targetExtension}");

                if (!File.Exists(sandboxFile))
                    throw new FileNotFoundException("Engine finished without crashes, but the expected output file was missing from the sandbox environment.");

                // 8. Move and rename to the final, collision-proof path
                File.Move(sandboxFile, finalTargetPath);

                return finalTargetPath;
            }
            finally
            {
                // 9. Nuke the sandbox to leave zero residual trace on the user's hard drive
                if (Directory.Exists(sandboxDir))
                {
                    try { Directory.Delete(sandboxDir, true); } catch { /* Prevent locking contentions from breaking the finally block */ }
                }
            }
        }
    }
}