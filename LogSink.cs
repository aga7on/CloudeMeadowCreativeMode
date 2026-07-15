using System.Collections.Generic;
// using System.Linq;
using UnityEngine;

namespace CloudMeadow.CreativeMode
{
    internal static class LogBuffer
    {
        private static readonly object _lock = new object();
        private static readonly Queue<string> _lines = new Queue<string>(256);
        private static readonly Queue<string> _errors = new Queue<string>(64);
        private const int Max = 200;

        public static void Add(string msg)
        {
            lock (_lock)
            {
                var s = "[" + Time.time.ToString("0.0") + "] " + msg;
                _lines.Enqueue(s);
                while (_lines.Count > Max) _lines.Dequeue();
            }
        }

        public static string[] Snapshot()
        {
            lock (_lock)
            {
                int n = _lines.Count;
                var arr = new string[n];
                int i = 0;
                foreach (var s in _lines)
                {
                    arr[i++] = s;
                }
                return arr;
            }
        }

        public static void AddError(string operation, string message, string stack)
        {
            lock (_lock)
            {
                string value = System.DateTime.Now.ToString("u") + " | " + operation + " | " + message + (string.IsNullOrEmpty(stack) ? "" : "\n" + stack);
                _errors.Enqueue(value); while (_errors.Count > 50) _errors.Dequeue();
            }
        }

        public static string[] ErrorSnapshot()
        {
            lock (_lock) { var result = new string[_errors.Count]; _errors.CopyTo(result, 0); return result; }
        }

        public static string ExportErrors(string path)
        {
            try { System.IO.File.WriteAllLines(path, ErrorSnapshot()); return path; }
            catch (System.Exception e) { return "FAILED: " + e.Message; }
        }
        public static void ClearErrors() { lock (_lock) _errors.Clear(); }
    }
}
