using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Glo; // Strict constraint: Reaching Glo.Vars.dataDIR

namespace WriterHead // Change this to your actual namespace
{
    public static class Writer
    {
        // 3 Operational Modes
        private enum WriteMode { Mode1_Nuke, Mode2_Log, Mode3_Settings }

        // The internal "Promise" Job class
        private class Job
        {
            public WriteMode Mode { get; set; }
            public object? LogEntry { get; set; }     // Nullable to fix CS8618
            public string? SysKey { get; set; }       // Nullable to fix CS8618
            public object? SysValue { get; set; }     // Nullable to fix CS8618
            public TaskCompletionSource<string> Tcs { get; set; } = null!; // Initialized to fix CS8618
        }

        // Gatekeeper control mechanisms
        private static readonly Queue<Job> _queue = new Queue<Job>();
        private static bool _isProcessing = false;
        private static CancellationTokenSource _cts = new CancellationTokenSource();
        private static readonly object _lock = new object();

        /// <summary>
        /// MODE 1: ROOT LEVEL PRIORITY. Wipes data.json entirely and defaults it.
        /// Stops whatever writer is doing right now and clears all promises.
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
                // STOP WHATEVER IS GOING ON: Cancel the active write operation
                _cts.Cancel();
                _cts = new CancellationTokenSource();

                // PROMISES ALL CLEARED: Nuke the queue entirely without completion
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
                    token = _cts.Token; // Capture token to respect Mode 1 cancellations
                }

                try
                {
                    // === CONSTRAINT ENFORCEMENT START ===
                    // Does it exist? Is it empty? Don't fix, don't create, don't scream. 
                    if (string.IsNullOrEmpty(Vars.dataDIR) || !File.Exists(Vars.dataDIR))
                    {
                        currentJob.Tcs.TrySetResult("data.json is not defined");
                        continue; 
                    }
                    // === CONSTRAINT ENFORCEMENT END ===

                    if (currentJob.Mode == WriteMode.Mode1_Nuke)
                    {
                        string defaultJson = @"{
  ""sys"": {
    ""openLog"": true,
    ""bg"": null,
    ""text"": null
  },
  ""logs"": [
  ]
}";
                        await File.WriteAllTextAsync(Vars.dataDIR, defaultJson, token);
                    }
                    else if (currentJob.Mode == WriteMode.Mode2_Log)
                    {
                        string json = await File.ReadAllTextAsync(Vars.dataDIR, token);
                        
                        // Fix for CS8600 & CS8602: Accept nullable return and check it.
                        JsonNode? root = JsonNode.Parse(json);
                        if (root == null) throw new InvalidOperationException("Parsed JSON is entirely null");
                        
                        JsonArray logs = root["logs"]?.AsArray() ?? new JsonArray();
                        
                        // Insert at index 0 (Pastes on top of logs [])
                        logs.Insert(0, JsonSerializer.SerializeToNode(currentJob.LogEntry));

                        var options = new JsonSerializerOptions { WriteIndented = true };
                        await File.WriteAllTextAsync(Vars.dataDIR, root.ToJsonString(options), token);
                    }
                    else if (currentJob.Mode == WriteMode.Mode3_Settings)
                    {
                        string json = await File.ReadAllTextAsync(Vars.dataDIR, token);
                        
                        // Fix for CS8600 & CS8602: Accept nullable return and check it.
                        JsonNode? root = JsonNode.Parse(json);
                        if (root == null) throw new InvalidOperationException("Parsed JSON is entirely null");
                        
                        JsonObject sys = root["sys"]?.AsObject() ?? new JsonObject();
                        
                        // Only updates 1 given setting (safe access to SysKey)
                        if (currentJob.SysKey != null)
                        {
                            sys[currentJob.SysKey] = JsonSerializer.SerializeToNode(currentJob.SysValue);
                        }

                        var options = new JsonSerializerOptions { WriteIndented = true };
                        await File.WriteAllTextAsync(Vars.dataDIR, root.ToJsonString(options), token);
                    }

                    // STRICT CONSTRAINT: "if you complete job correctly , just return 'success' thats it"
                    currentJob.Tcs.TrySetResult("success");
                }
                catch (OperationCanceledException)
                {
                    // Mode 1 nuked this job while it was actively reading/writing. Clear the promise.
                    currentJob.Tcs.TrySetCanceled();
                }
                catch (Exception)
                {
                    // STRICT CONSTRAINT: "no superhero shit"
                    // If parsing corrupted JSON fails (or if we threw InvalidOperationException above),
                    // it does not fix it or crash. Exits silently with simple error string.
                    currentJob.Tcs.TrySetResult("error");
                }
            }
        }
    }
}