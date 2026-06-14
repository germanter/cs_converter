using System;
using WriterHead; 

namespace AppLogger 
{
    public static class Logger
    {
        /// <summary>
        /// Creates a log object, passes it to Mode2_WriteLogAsync, and forgets it.
        /// Non-blocking, completely silent on failure.
        /// </summary>
        public static void Log(string type, string fullpath, string status)
        {
            try
            {
                // Create the exact JSON structure. 
                // Takes your strings directly. UUID and timestamp generated internally.
                var logEntry = new
                {
                    uuid = Guid.NewGuid().ToString("N").Substring(0, 8),
                    type = type,
                    fullpath = fullpath,
                    status = status,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ssfff")
                };

                // Pass to Mode 2 and immediately forget it
                _ = Writer.Mode2_WriteLogAsync(logEntry);
            }
            catch
            {
                // Silently dies.
            }
        }
    }
}