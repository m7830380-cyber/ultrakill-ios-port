using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UltrakillIOS
{
    /// <summary>
    /// Mirrors every Unity log message to Documents/ULTRAKILL-Logs/session-*.txt and keeps a tail for the on-screen console.
    /// </summary>
    public static class UltrakillLog
    {
        public const string LogFolderName = "ULTRAKILL-Logs";
        private const int TailCapacity = 300;
        private const int KeepSessions = 10;

        private static readonly object Sync = new object();
        private static readonly Queue<string> Tail = new Queue<string>();
        private static StreamWriter _writer;
        private static bool _initialized;

        public static string LogFilePath { get; private set; }
        public static int ErrorCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            Application.logMessageReceivedThreaded += OnLog;
            Application.quitting += Close;

            try
            {
                var folder = Path.Combine(ExternalContentBootstrap.GetDocumentsPath(), LogFolderName);
                Directory.CreateDirectory(folder);
                PruneOldSessions(folder);
                LogFilePath = Path.Combine(folder, "session-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
                _writer = new StreamWriter(new FileStream(LogFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    AutoFlush = true
                };
            }
            catch (Exception ex)
            {
                AddToTail("[UltrakillIOS] Could not open log file: " + ex.Message);
            }

            Info("Log", "Session started " + DateTime.Now.ToString("u"));
            Info("Log", "Unity " + Application.unityVersion + ", app " + Application.version + ", platform " + Application.platform);
            Info("Log", "Device " + SystemInfo.deviceModel + ", OS " + SystemInfo.operatingSystem + ", RAM " + SystemInfo.systemMemorySize + " MB");
            Info("Log", "GPU " + SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsDeviceType + ")");
#if ULTRAKILL_FULL_PORT
            Info("Log", "Build: ULTRAKILL_FULL_PORT defined");
#else
            Info("Log", "Build: ULTRAKILL_FULL_PORT NOT defined (engine shell)");
#endif
            Info("Log", "Debug build: " + Debug.isDebugBuild);
            Info("Log", "Log file: " + (LogFilePath ?? "<none>"));
        }

        public static void Info(string area, string message)
        {
            Debug.Log("[UltrakillIOS:" + area + "] " + message);
        }

        public static void Warn(string area, string message)
        {
            Debug.LogWarning("[UltrakillIOS:" + area + "] " + message);
        }

        public static void Error(string area, string message)
        {
            Debug.LogError("[UltrakillIOS:" + area + "] " + message);
        }

        public static string[] GetTail(int count)
        {
            lock (Sync)
            {
                return Tail.Skip(Math.Max(0, Tail.Count - count)).ToArray();
            }
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            var prefix = type switch
            {
                LogType.Error => "E",
                LogType.Exception => "X",
                LogType.Assert => "A",
                LogType.Warning => "W",
                _ => "I"
            };

            var line = DateTime.Now.ToString("HH:mm:ss.fff") + " " + prefix + " " + condition;
            lock (Sync)
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                {
                    ErrorCount++;
                }

                AddToTailLocked(line);
                if (_writer == null)
                {
                    return;
                }

                try
                {
                    _writer.WriteLine(line);
                    if ((type == LogType.Exception || type == LogType.Error) && !string.IsNullOrEmpty(stackTrace))
                    {
                        _writer.WriteLine(stackTrace.TrimEnd());
                    }
                }
                catch
                {
                    _writer = null;
                }
            }
        }

        private static void AddToTail(string line)
        {
            lock (Sync)
            {
                AddToTailLocked(line);
            }
        }

        private static void AddToTailLocked(string line)
        {
            Tail.Enqueue(line);
            while (Tail.Count > TailCapacity)
            {
                Tail.Dequeue();
            }
        }

        private static void PruneOldSessions(string folder)
        {
            var old = new DirectoryInfo(folder).GetFiles("session-*.txt")
                .OrderByDescending(f => f.CreationTimeUtc)
                .Skip(KeepSessions - 1);
            foreach (var file in old)
            {
                try
                {
                    file.Delete();
                }
                catch
                {
                }
            }
        }

        private static void Close()
        {
            lock (Sync)
            {
                _writer?.Dispose();
                _writer = null;
            }
        }
    }
}
