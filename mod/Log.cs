using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace UltrakillIOS
{
    // Mirrors the Unity log into Documents/ULTRAKILL-iOS.log so it can be pulled off the device with
    // the Files app / Filza, and keeps the last lines for the on-screen overlay.
    public static class Log
    {
        static StreamWriter s_Writer;
        static readonly object s_Lock = new object();
        public static readonly Queue<string> Recent = new Queue<string>();
        public static int ErrorCount;
        static string s_LastMessage;
        static int s_Repeats;

        public static void Init()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, "ULTRAKILL-iOS.log");
                string prev = Path.Combine(Application.persistentDataPath, "ULTRAKILL-iOS-prev.log");
                if (File.Exists(path))
                {
                    if (File.Exists(prev)) File.Delete(prev);
                    File.Move(path, prev);
                }
                s_Writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite), new UTF8Encoding(false));
                s_Writer.AutoFlush = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[UltrakillIOS] cannot open log file: " + e.Message);
            }
            Application.logMessageReceivedThreaded += OnLog;
        }

        static void OnLog(string condition, string stackTrace, LogType type)
        {
            lock (s_Lock)
            {
                if (condition == s_LastMessage)
                {
                    // the game can spam the same warning every frame; collapse repeats
                    s_Repeats++;
                    if (s_Repeats != 10 && s_Repeats != 100 && s_Repeats % 1000 != 0) return;
                    condition = $"(repeated x{s_Repeats}) " + condition;
                }
                else
                {
                    s_LastMessage = condition;
                    s_Repeats = 0;
                }
                string tag = type == LogType.Log ? "I" : type == LogType.Warning ? "W" : "E";
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) ErrorCount++;
                string line = $"{DateTime.Now:HH:mm:ss.fff} [{tag}] {condition}";
                try
                {
                    s_Writer?.WriteLine(line);
                    if ((type == LogType.Exception || type == LogType.Error) && !string.IsNullOrEmpty(stackTrace))
                        s_Writer?.WriteLine(stackTrace.TrimEnd());
                }
                catch { }
                Recent.Enqueue(line.Length > 300 ? line.Substring(0, 300) : line);
                while (Recent.Count > 40) Recent.Dequeue();
            }
        }

        public static string Snapshot()
        {
            lock (s_Lock) { return string.Join("\n", Recent.ToArray()); }
        }

        public static void Info(string s) => Debug.Log("[UltrakillIOS] " + s);
        public static void Warn(string s) => Debug.LogWarning("[UltrakillIOS] " + s);
        public static void Error(string s) => Debug.LogError("[UltrakillIOS] " + s);
    }
}
