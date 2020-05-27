using System;
using System.IO;
using System.Text;

namespace MigrationContextUnpack.Sources.Tracing
{
    public enum LogLevel
    {
        Trace = 0,
        Debug,
        Warning,
        Error
    }

    public class Logger
    {
        public static string LogFilePath 
        {
            get { return logFilePath; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException("Log file path must be defined!");
                logFilePath = value;
                var parentFolder = Path.GetDirectoryName(value);
                if (!Directory.Exists(parentFolder)) Directory.CreateDirectory(parentFolder);
            }
        }
        private static string logFilePath;
        
        private static object writeLock = new object();

        public static void Log(string message, bool isHeader, LogLevel level = LogLevel.Trace)
        {
            lock (writeLock)
            {
                var lines = new StringBuilder();
                
                var linesArray = !string.IsNullOrEmpty(message) ?
                        message.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries):
                        new string[] { string.Empty }; 

                if (linesArray.Length == 0) return;

                var i = 0;

                if (isHeader)
                {
                    lines.Insert(0, Environment.NewLine + ("--- " + linesArray[0] + " ").PadRight(500, '-') + Environment.NewLine + Environment.NewLine);
                    i = 1;
                }

                while (i < linesArray.Length)
                {
                    var output = string.Format("{0} | {1,-10} | {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff"), level, linesArray[i]);
                    lines.AppendLine(output);
                    i++;
                }

                Console.Write(lines.ToString());
                if (!string.IsNullOrEmpty(logFilePath))
                {
                    StreamWriter writer = null;
                    try
                    {
                        writer = new StreamWriter(logFilePath, true, Encoding.UTF8);
                        writer.Write(lines.ToString());
                        writer.Flush();
                    }
                    catch { }
                    finally { if (writer != null) writer.Dispose(); }
                }
            }
        }

        public static void Log(LogLevel level = LogLevel.Trace)
        {
            Log(string.Empty, false, level);
        }
        public static void Log(string message, LogLevel level = LogLevel.Trace)
        {
            Log(message, false, level);
        }
        public static void Log(string format, string[] args, LogLevel level = LogLevel.Trace) 
        {
            Log(string.Format(format, args), level);
        }
        public static void Log(string format, string arg, LogLevel level = LogLevel.Trace) 
        {
            Log(string.Format(format, arg), level);
        }
        public static void Log(string format, string arg1, string arg2, LogLevel level = LogLevel.Trace) 
        {
            Log(string.Format(format, arg1, arg2), level);
        }
        public static void Log(string format, string arg1, string arg2, string arg3, LogLevel level = LogLevel.Trace) 
        {
            Log(string.Format(format, arg1, arg2, arg3), level);
        }
        
    }
}
