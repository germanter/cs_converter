
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Glo; // Strict constraint: Reaching Glo.Vars.dataDIR

namespace WriterHead
{
    public static class Writer
    {
        // === CONFIGURABLE DEFAULTS AT TOP ===
        public static readonly string DefaultFullJson = @"{
  ""sys"": {
    ""openLog"": true,
    ""bg"": ""#1E1E1E"",
    ""text"": ""#FFFFFF""
  },
  ""logs"": []
}";

        public static readonly string DefaultSysJson = @"{
  ""openLog"": true,
    ""bg"": ""#1E1E1E"",
    ""text"": ""#FFFFFF""
}";

        // Log limitation variables
        public static readonly int limitOnLog = 100;
        public static readonly int limitOffLog = 1;

        // System Level Initialization Return Object
        public class InitResult
        {
            public bool status { get; set; }
            public string content { get; set; } = "{}";
        }

        /// <summary>
        /// Synchronous Core Initializer. Checks file existence, validity, and the integrity of the sys config.
        /// </summary>
        public static InitResult Initializer()
        {
            // 1. Guardrail: Verify if file exists in the directory. DO NOT auto-create.
            if (string.IsNullOrEmpty(Vars.dataDIR) || !File.Exists(Vars.dataDIR))
            {
                return new InitResult { status = false, content = "{}" };
            }

            try
            {
                string rawJson;
                try
                {
                    rawJson = File.ReadAllText(Vars.dataDIR);
                }
                catch
                {
                    // Cannot open the file (e.g., lock issues or permission issues)
                    return new InitResult { status = false, content = "{}" };
                }

                bool isCorrupt = false;
                JsonNode? root = null;
                JsonObject? sys = null;

                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    isCorrupt = true;
                }
                else
                {
                    try
                    {
                        root = JsonNode.Parse(rawJson);
                        if (root == null)
                        {
                            isCorrupt = true;
                        }
                        else
                        {
                            sys = root["sys"] as JsonObject;
                            var logs = root["logs"] as JsonArray;

                            if (sys == null || logs == null)
                            {
                                isCorrupt = true;
                            }
                            else
                            {
                                // Validate sys keys: openLog, bg, text
                                if (!sys.TryGetPropertyValue("openLog", out var openLogNode) || openLogNode == null ||
                                    (openLogNode.GetValueKind() != JsonValueKind.True && openLogNode.GetValueKind() != JsonValueKind.False))
                                {
                                    isCorrupt = true;
                                }
                                else if (!sys.TryGetPropertyValue("bg", out var bgNode) || bgNode == null ||
                                         bgNode.GetValueKind() != JsonValueKind.String || !IsValidHexColor(bgNode.ToString()))
                                {
                                    isCorrupt = true;
                                }
                                else if (!sys.TryGetPropertyValue("text", out var textNode) || textNode == null ||
                                         textNode.GetValueKind() != JsonValueKind.String || !IsValidHexColor(textNode.ToString()))
                                {
                                    isCorrupt = true;
                                }
                            }
                        }
                    }
                    catch
                    {
                        isCorrupt = true;
                    }
                }

                if (isCorrupt)
                {
                    // Nuke the existing corrupted file and write default JSON
                    File.WriteAllText(Vars.dataDIR, DefaultFullJson);
                    
                    // Synchronously sync the in-memory snapshot
                    Vars.jsonSnapshot = ReadProperJsonSync(Vars.dataDIR);

                    var freshRoot = JsonNode.Parse(DefaultFullJson);
                    string freshSys = freshRoot?["sys"]?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "{}";

                    return new InitResult { status = true, content = freshSys };
                }
                else
                {
                    // Sync the snapshot with current verified file content
                    Vars.jsonSnapshot = ReadProperJsonSync(Vars.dataDIR);

                    string sysContent = sys!.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                    return new InitResult { status = true, content = sysContent };
                }
            }
            catch
            {
                // Unrecoverable parsing/file system crash fallback
                return new InitResult { status = false, content = "{}" };
            }
        }

        private static bool IsValidHexColor(string? color)
        {
            if (string.IsNullOrWhiteSpace(color)) return false;
            // Matches standard hex color strings (e.g., #FFF, #1E1E1E, #FF1E1E1E)
            return System.Text.RegularExpressions.Regex.IsMatch(color, @"^#([0-9a-fA-F]{6}|[0-9a-fA-F]{8}|[0-9a-fA-F]{3})$");
        }

        private static string ReadProperJsonSync(string path)
        {
            try
            {
                if (!File.Exists(path)) return "";

                string raw = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(raw)) return "";

                JsonNode? root = JsonNode.Parse(raw);
                if (root == null) return "";

                if (!(root["sys"] is JsonObject sys)) return "";
                if (!(root["logs"] is JsonArray logs)) return "";

                var cleanRoot = new JsonObject
                {
                    ["sys"] = sys.DeepClone(),
                    ["logs"] = logs.DeepClone()
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                return cleanRoot.ToJsonString(options);
            }
            catch
            {
                return "";
            }
        }

        // 5 Operational Modes
        private enum WriteMode
        {
            Mode1_Nuke,
            Mode2_Log,
            Mode3_Settings,
            Mode4_NukeSys,
            Mode5_NukeLogs
        }

        // The internal "Promise" Job class
        private class Job
        {
            public WriteMode Mode { get; set; }
            public object? LogEntry { get; set; }     
            public string? SysKey { get; set; }       
            public object? SysValue { get; set; }     
            public List<string>? WhereList { get; set; }
            public TaskCompletionSource<string> Tcs { get; set; } = null!; 
        }

        // Gatekeeper control mechanisms
        private static readonly Queue<Job> _queue = new Queue<Job>();
        private static bool _isProcessing = false;
        private static CancellationTokenSource _cts = new CancellationTokenSource();
        private static readonly object _lock = new object();

        private static async Task<string> ReadProperJsonAsync(string path, CancellationToken token)
        {
            try
            {
                if (!File.Exists(path)) return "";

                string raw = await File.ReadAllTextAsync(path, token);
                if (string.IsNullOrWhiteSpace(raw)) return "";

                JsonNode? root = JsonNode.Parse(raw);
                if (root == null) return "";

                // Strict validation: must contain sys (object) and logs (array)
                if (!(root["sys"] is JsonObject sys)) return "";
                if (!(root["logs"] is JsonArray logs)) return "";

                // Build a fresh, clean root. This automatically filters out 
                // any non-existent/random keys that might have slipped into the file.
                var cleanRoot = new JsonObject
                {
                    ["sys"] = sys.DeepClone(),
                    ["logs"] = logs.DeepClone()
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                return cleanRoot.ToJsonString(options);
            }
            catch
            {
                // Swallow any file lock exceptions, parsing errors, or deep clone failures.
                // Returning an empty string prevents the UI from showing broken text.
                return "";
            }
        }

        /// <summary>
        /// MODE 1: ROOT LEVEL PRIORITY. Wipes data.json entirely and defaults it.
        /// </summary>
        public static Task<string> Mode1_NukeDataAsync()
        {
            var job = new Job
            {
                Mode = WriteMode.Mode1_Nuke,
                Tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously)
            };
            EnqueueNuclearJob(job);
            return job.Tcs.Task;
        }

        /// <summary>
        /// MODE 2: Pastes 1 new log on top of the "logs" [] array, keeps rest same.
        /// </summary>
        public static Task<string> Mode2_WriteLogAsync(object? newLog)
        {
            var job = new Job
            {
                Mode = WriteMode.Mode2_Log,
                LogEntry = newLog,
                Tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously)
            };
            EnqueueStandardJob(job);
            return job.Tcs.Task;
        }

        /// <summary>
        /// MODE 3: Changes 1 setting in "sys" (e.g. openLog, bg, text), keeps rest same.
        /// </summary>
        public static Task<string> Mode3_UpdateSettingAsync(string key, object? value)
        {
            var job = new Job
            {
                Mode = WriteMode.Mode3_Settings,
                SysKey = key,
                SysValue = value,
                Tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously)
            };
            EnqueueStandardJob(job);
            return job.Tcs.Task;
        }

        /// <summary>
        /// MODE 4: Defaults ONLY the "sys" object back to DefaultSysJson, leaves logs alone.
        /// </summary>
        public static Task<string> Mode4_NukeSysAsync()
        {
            var job = new Job
            {
                Mode = WriteMode.Mode4_NukeSys,
                Tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously)
            };
            EnqueueStandardJob(job);
            return job.Tcs.Task;
        }

        /// <summary>
        /// MODE 5: Deletes logs. If "where" is empty/null, nukes ALL logs. 
        /// If "where" contains UUIDs, removes only those specific logs.
        /// </summary>
        public static Task<string> Mode5_NukeLogsAsync(List<string>? where = null)
        {
            var job = new Job
            {
                Mode = WriteMode.Mode5_NukeLogs,
                WhereList = where,
                Tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously)
            };
            EnqueueStandardJob(job);
            return job.Tcs.Task;
        }

        // --- INTERNAL GATEKEEPER LOGIC ---

        private static void EnqueueStandardJob(Job job)
        {
            lock (_lock)
            {
                _queue.Enqueue(job);
                if (!_isProcessing)
                {
                    _isProcessing = true;
                    _ = Task.Run(() => ProcessQueueAsync());
                }
            }
        }

        private static void EnqueueNuclearJob(Job job)
        {
            lock (_lock)
            {
                // STOP WHATEVER IS GOING ON
                _cts.Cancel();
                _cts = new CancellationTokenSource();

                // PROMISES ALL CLEARED
                while (_queue.Count > 0)
                {
                    var pending = _queue.Dequeue();
                    pending.Tcs.TrySetCanceled(); 
                }

                // Add only the nuke job
                _queue.Enqueue(job);

                if (!_isProcessing)
                {
                    _isProcessing = true;
                    _ = Task.Run(() => ProcessQueueAsync());
                }
            }
        }

        private static async Task ProcessQueueAsync()
        {
            while (true)
            {
                Job currentJob;
                CancellationToken token;

                lock (_lock)
                {
                    if (_queue.Count == 0)
                    {
                        _isProcessing = false;
                        return;
                    }
                    currentJob = _queue.Dequeue();
                    token = _cts.Token; 
                }

                try
                {
                    // === CONSTRAINT ENFORCEMENT START ===
                    // File check: Does it exist? Don't create, don't scream. 
                    if (string.IsNullOrEmpty(Vars.dataDIR) || !File.Exists(Vars.dataDIR))
                    {
                        currentJob.Tcs.TrySetResult("data.json is not defined");
                        continue; 
                    }

                    // Mode 1: Ultimate priority. No read needed. Just wipe it clean.
                    if (currentJob.Mode == WriteMode.Mode1_Nuke)
                    {
                        await File.WriteAllTextAsync(Vars.dataDIR, DefaultFullJson, token);
                        // NEW: Read the exact state from disk after nuking
                        Vars.jsonSnapshot = await ReadProperJsonAsync(Vars.dataDIR, token);
                        currentJob.Tcs.TrySetResult("success");
                        continue;
                    }

                    // For all other modes, read and validate the JSON.
                    string json = await File.ReadAllTextAsync(Vars.dataDIR, token);
                    
                    // If file is entirely empty, this triggers the corruption fallback
                    if (string.IsNullOrWhiteSpace(json)) 
                        throw new InvalidDataException("Empty File"); 

                    JsonNode? root = JsonNode.Parse(json);
                    
                    // If file is not valid JSON, this triggers the corruption fallback
                    if (root == null) 
                        throw new InvalidDataException("Null JSON");

                    // Schema validation: If the user messed with the architecture, trigger corruption fallback
                    if (!(root["sys"] is JsonObject sys)) throw new InvalidDataException("Broken sys schema");
                    if (!(root["logs"] is JsonArray logs)) throw new InvalidDataException("Broken logs schema");

                    // Execute Modes
                    if (currentJob.Mode == WriteMode.Mode2_Log)
                    {
                        // Insert new log onto the top (index 0) of the array
                        logs.Insert(0, JsonSerializer.SerializeToNode(currentJob.LogEntry));

                        // Read the 'openLog' flag inside the 'sys' object to determine limit
                        bool openLogValue = true; // default fallback if unreadable
                        if (sys.TryGetPropertyValue("openLog", out var openLogNode) && openLogNode != null)
                        {
                            var kind = openLogNode.GetValueKind();
                            if (kind == JsonValueKind.True) openLogValue = true;
                            else if (kind == JsonValueKind.False) openLogValue = false;
                            else if (kind == JsonValueKind.String)
                            {
                                if (bool.TryParse(openLogNode.ToString(), out bool parsedBool))
                                {
                                    openLogValue = parsedBool;
                                }
                            }
                        }

                        // Select the appropriate limit based on 'openLog'
                        int limit = openLogValue ? limitOnLog : limitOffLog;

                        // Delete from the bottom (oldest) of the array until we hit the limit
                        while (logs.Count > limit)
                        {
                            logs.RemoveAt(logs.Count - 1);
                        }
                    }
                    else if (currentJob.Mode == WriteMode.Mode3_Settings)
                    {
                        if (currentJob.SysKey != null)
                        {
                            sys[currentJob.SysKey] = JsonSerializer.SerializeToNode(currentJob.SysValue);
                        }
                    }
                    else if (currentJob.Mode == WriteMode.Mode4_NukeSys)
                    {
                        root["sys"] = JsonNode.Parse(DefaultSysJson);
                    }
                    else if (currentJob.Mode == WriteMode.Mode5_NukeLogs)
                    {
                        if (currentJob.WhereList == null || currentJob.WhereList.Count == 0)
                        {
                            // Truncate entirely
                            root["logs"] = new JsonArray();
                        }
                        else
                        {
                            // Target specific UUIDs. Loop backwards to safely remove elements.
                            for (int i = logs.Count - 1; i >= 0; i--)
                            {
                                var node = logs[i];
                                // STRICT CONSTRAINT: Check ONLY for "uuid", no fallback to "id"
                                if (node is JsonObject obj && obj.TryGetPropertyValue("uuid", out var uuidNode))
                                {
                                    string? uuidStr = uuidNode?.ToString();
                                    if (uuidStr != null && currentJob.WhereList.Contains(uuidStr))
                                    {
                                        logs.RemoveAt(i);
                                    }
                                }
                            }
                        }
                    }

                    // Write successfully executed manipulations back to file
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    await File.WriteAllTextAsync(Vars.dataDIR, root.ToJsonString(options), token);
                    // NEW: Stop guessing. Read the truth from the newly saved file.
                    Vars.jsonSnapshot = await ReadProperJsonAsync(Vars.dataDIR, token);

                    currentJob.Tcs.TrySetResult("success");
                }
                catch (OperationCanceledException)
                {
                    // Mode 1 nuked this job while it was actively working.
                    currentJob.Tcs.TrySetCanceled();
                }
                catch (Exception)
                {
                    try
                    {
                        await File.WriteAllTextAsync(Vars.dataDIR, DefaultFullJson, token);
                        
                        // NEW: Even in a crash, we read the fresh defaulted file back from disk
                        Vars.jsonSnapshot = await ReadProperJsonAsync(Vars.dataDIR, token);
                    }
                    catch { } // No superhero actions. If we can't write, silently walk away.

                    currentJob.Tcs.TrySetResult("success");
                }
            }
        }
    }
}