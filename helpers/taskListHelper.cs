using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Glo;

namespace convix
{
    public static class TaskListHelper
    {
        /// <summary>
        /// Reads the JSON snapshot in the background, extracts the logs based on the mapped category name, 
        /// and returns the successful tasks' fullPath and uuid. 
        /// If ANY error happens, it silently crashes and returns the word "fail".
        /// </summary>
        public static async Task<(string Status, List<(string FullPath, string Uuid)> Tasks)> GetCompletedTasksAsync(string categoryName, string jsonSnapshot)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // 1. Map the UI category name to the internal data name (e.g., Image2PDF -> image2pdf)
                    if (!Vars.ctgMap.TryGetValue(categoryName, out string? mappedCategory) || mappedCategory == null)
                    {
                        return ("fail", new List<(string FullPath, string Uuid)>());
                    }

                    // 2. Parse the JSON snapshot
                    using JsonDocument doc = JsonDocument.Parse(jsonSnapshot);
                    
                    // 3. Find the "logs" array
                    if (!doc.RootElement.TryGetProperty("logs", out JsonElement logsElement) || logsElement.ValueKind != JsonValueKind.Array)
                    {
                        return ("fail", new List<(string FullPath, string Uuid)>());
                    }

                    var results = new List<(string FullPath, string Uuid)>();

                    // 4. Iterate over the logs array
                    foreach (JsonElement logItem in logsElement.EnumerateArray())
                    {
                        // Grab type and status safely (Strictly lower-case keys based on data.json)
                        string type = logItem.TryGetProperty("type", out JsonElement typeElement) ? typeElement.GetString() ?? "" : "";
                        string status = logItem.TryGetProperty("status", out JsonElement statusElement) ? statusElement.GetString() ?? "" : "";

                        // Filter by type (mappedCategory) AND status ("success")
                        if (type == mappedCategory && status == "success")
                        {
                            // Try to get fullpath and uuid (Must be exactly "fullpath" to match data.json!)
                            string fullPath = logItem.TryGetProperty("fullpath", out JsonElement fpElement) ? fpElement.GetString() ?? "" : "";
                            string uuid = logItem.TryGetProperty("uuid", out JsonElement uuidElement) ? uuidElement.GetString() ?? "" : "";

                            results.Add((fullPath, uuid));
                        }
                    }

                    return ("success", results);
                }
                catch
                {
                    // Silently crash and return the "fail" word
                    return ("fail", new List<(string FullPath, string Uuid)>());
                }
            });
        }

        /// <summary>
        /// Tries to delete a file exactly ONCE in the background. 
        /// </summary>
        public static async Task DeleteFileAsync(string filePath)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch
                {
                    // Don't care, don't try again, don't scream. Just walk away.
                }
            });
        }
    }
}