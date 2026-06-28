using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Glo;
using CentralGateway;
using WriterHead;

namespace Destruction
{
    public static class Destructor
    {
        /// <summary>
        /// Executes a top-level destruction sequence. 
        /// Orders CentralController and Writer to stop gracefully, verifies directory ownership, 
        /// safely deletes the workspace directory, and terminates the application process.
        /// Returns false if any part of the verification or halt sequence fails.
        /// </summary>
        public static async Task<bool> Destroy()
        {
            // 1. Send the halt/nuke signal to CentralController
            CentralController.nuke = true;

            // Wait defensively for any running operations to shut down (up to 10 seconds)
            int controllerTimeoutMs = 10000;
            int elapsedMs = 0;
            int intervalMs = 100;

            while (CentralController.isRunning)
            {
                if (elapsedMs >= controllerTimeoutMs)
                {
                    // CentralController failed to halt within the allotted timeout
                    return false;
                }
                await Task.Delay(intervalMs);
                elapsedMs += intervalMs;
            }

            // 2. Terminate and finalize the database writer queue
            try
            {
                var nukeTask = Writer.Mode1_NukeDataAsync();
                var completedTask = await Task.WhenAny(nukeTask, Task.Delay(5000));

                if (completedTask != nukeTask)
                {
                    // Writer operation timed out (e.g., heavily queued operations hung)
                    return false;
                }

                string writerResult = await nukeTask;
                if (writerResult != "success")
                {
                    // Writer failed to execute its final backup/default routine
                    return false;
                }
            }
            catch
            {
                // Caught filesystem exception or queue communication issue
                return false;
            }

            // 3. Inspect target directory and verify ownership signatures
            string mainDir = Vars.mainDIR;
            if (string.IsNullOrWhiteSpace(mainDir) || !Directory.Exists(mainDir))
            {
                return false;
            }

            // Validate that we can safely read the files inside the workspace (read privilege check)
            try
            {
                _ = Directory.GetFiles(mainDir);
            }
            catch
            {
                return false;
            }

            // Clean the path of trailing separators to accurately resolve the folder name
            string folderName = Path.GetFileName(mainDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            string signatureName = $".4A1F9B2C_{folderName}";
            string signaturePath = Path.Combine(mainDir, signatureName);

            if (!File.Exists(signaturePath))
            {
                // Verification mismatch: Folder is not recognized as owned by this application
                return false;
            }

            // 4. Safely delete the workspace folder with a retry/backoff mechanism 
            try
            {
                bool successfullyDeleted = false;

                // Attempt recursive deletion over multiple passes to mitigate asynchronous file lock delays
                for (int attempt = 0; attempt < 5; attempt++)
                {
                    try
                    {
                        if (Directory.Exists(mainDir))
                        {
                            Directory.Delete(mainDir, true);
                        }
                        successfullyDeleted = true;
                        break;
                    }
                    catch
                    {
                        // Wait briefly for active handles or process terminations to release
                        await Task.Delay(500); 
                    }
                }

                if (!successfullyDeleted)
                {
                    // Persistent locks prevented directory removal
                    return false;
                }
            }
            catch
            {
                return false;
            }

            // 5. Force close the application upon complete execution of directory purging
            Environment.Exit(0);
            return true;
        }
    }
}