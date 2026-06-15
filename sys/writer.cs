
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
                        logs.Insert(0, JsonSerializer.SerializeToNode(currentJob.LogEntry));
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

                    currentJob.Tcs.TrySetResult("success");
                }
                catch (OperationCanceledException)
                {
                    // Mode 1 nuked this job while it was actively working.
                    currentJob.Tcs.TrySetCanceled();
                }
                catch (Exception)
                {
                    // =========================================================
                    // EXTREME PROTOCOL LEVEL IMPORTANCE: CORRUPTION FALLBACK
                    // Any error above (empty, parse fail, schema break) lands here.
                    // DO NOT EXECUTE THE GIVEN ACTUAL TASK. JUST NUKE IT AND MOVE AWAY.
                    // =========================================================
                    try
                    {
                        await File.WriteAllTextAsync(Vars.dataDIR, DefaultFullJson, token);
                    }
                    catch { } // No superhero shit. If we can't write, silently walk away.

                    currentJob.Tcs.TrySetResult("success");
                }
            }
        }
    }
}