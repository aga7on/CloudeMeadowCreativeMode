using System.Collections.Generic;
// using System.Linq;
using UnityEngine;

namespace CloudMeadow.CreativeMode
{
    internal static class LogBuffer
    {
        private static readonly object _lock = new object();
        private static readonly Queue<string> _lines = new Queue<string>(256);
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
    }
}
