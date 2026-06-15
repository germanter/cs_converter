using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Glo;

namespace convix
{
    public static class TaskListHelper
    {
        public static async Task<(string Status, List<(string FullPath, string Uuid)> Tasks)> GetCompletedTasksAsync(string categoryName, string jsonSnapshot, CancellationToken ct = default)
        {
            // FIX: Avoid huge allocation dumps on null hits
            if (string.IsNullOrWhiteSpace(jsonSnapshot) || string.IsNullOrWhiteSpace(categoryName))
                return ("fail", new List<(string, string)>(0)); 

            // FIX: Prevent devastating process crash if dictionary is null
            if (Vars.ctgMap == null || !Vars.ctgMap.TryGetValue(categoryName, out string? mappedCategory) || mappedCategory == null)
                return ("fail", new List<(string, string)>(0));

            return await Task.Run(() =>
            {
                try
                {
                    if (ct.IsCancellationRequested) return ("fail", new List<(string, string)>(0));

                    using JsonDocument doc = JsonDocument.Parse(jsonSnapshot);
                    
                    if (!doc.RootElement.TryGetProperty("logs", out JsonElement logsElement) || logsElement.ValueKind != JsonValueKind.Array)
                        return ("fail", new List<(string, string)>(0));

                    // OPTIMIZATION: Extract exact buffer size to stop memory fragmentation reallocating
                    var results = new List<(string FullPath, string Uuid)>(logsElement.GetArrayLength());

                    foreach (JsonElement logItem in logsElement.EnumerateArray())
                    {
                        if (ct.IsCancellationRequested) break;

                        string type = logItem.TryGetProperty("type", out JsonElement typeElement) ? typeElement.GetString() ?? "" : "";
                        string status = logItem.TryGetProperty("status", out JsonElement statusElement) ? statusElement.GetString() ?? "" : "";

                        // OPTIMIZATION: Direct memory lookup logic via Ordinal avoids slow CPU cultural string comparisons
                        if (string.Equals(type, mappedCategory, StringComparison.Ordinal) && 
                            string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
                        {
                            string fullPath = logItem.TryGetProperty("fullpath", out JsonElement fpElement) ? fpElement.GetString() ?? "" : "";
                            string uuid = logItem.TryGetProperty("uuid", out JsonElement uuidElement) ? uuidElement.GetString() ?? "" : "";
                            
                            // FIX: Prevent ghosts pushing invalid UI lines
                            if (!string.IsNullOrEmpty(uuid) && !string.IsNullOrEmpty(fullPath)) 
                                results.Add((fullPath, uuid));
                        }
                    }

                    results.TrimExcess(); // OPTIMIZATION: Purge bloated memory left from the initial buffer size
                    return ("success", results);
                }
                catch
                {
                    return ("fail", new List<(string, string)>(0));
                }
            }, ct); // FIX: Bound CancellationToken immediately handles memory correctly
        }

        public static async Task DeleteFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(filePath)) 
                    {
                        // FIX: Safely override ReadOnly attribute which ordinarily throws UnauthorizedAccessException 
                        File.SetAttributes(filePath, FileAttributes.Normal);
                        File.Delete(filePath);
                    }
                }
                catch { /* FIX: Walk away silently */ }
            });
        }
    }
}