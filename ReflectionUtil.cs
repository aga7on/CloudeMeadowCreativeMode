using System;
using System.Collections;
using System.Collections.Generic;
// using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CloudMeadow.CreativeMode
{
    internal static class ReflectionUtil
    {
        // Finds MonoBehaviour instances whose type name contains any of the provided tokens
        public static List<MonoBehaviour> FindMonoBehaviours(params string[] typeNameTokens)
        {
            var all = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
            // lower tokens
            string[] tokens = new string[typeNameTokens.Length];
            for (int i = 0; i < typeNameTokens.Length; i++) tokens[i] = (typeNameTokens[i] ?? string.Empty).ToLowerInvariant();
            var list = new List<MonoBehaviour>();
            for (int i = 0; i < all.Length; i++)
            {
                var mb = all[i];
                var n = (mb.GetType().FullName ?? string.Empty).ToLowerInvariant();
                for (int t = 0; t < tokens.Length; t++)
                {
                    if (n.IndexOf(tokens[t], StringComparison.Ordinal) >= 0)
                    {
                        list.Add(mb);
                        break;
                    }
                }
            }
            return list;
        }

        // Finds types in loaded assemblies by name tokens
        public static IEnumerable<Type> FindTypes(params string[] nameTokens)
        {
            // lower tokens
            string[] tokens = new string[nameTokens.Length];
            for (int i = 0; i < nameTokens.Length; i++) tokens[i] = (nameTokens[i] ?? string.Empty).ToLowerInvariant();
            var asms = AppDomain.CurrentDomain.GetAssemblies();
            for (int a = 0; a < asms.Length; a++)
            {
                Type[] types;
                try { types = asms[a].GetTypes(); } catch { continue; }
                for (int i = 0; i < types.Length; i++)
                {
                    var t = types[i];
                    var n = ((t.FullName ?? t.Name) ?? string.Empty).ToLowerInvariant();
                    for (int tok = 0; tok < tokens.Length; tok++)
                    {
                        if (n.IndexOf(tokens[tok], StringComparison.Ordinal) >= 0)
                        {
                            yield return t;
                            break;
                        }
                    }
                }
            }
        }

        // Tries to invoke any static method that looks like an unlocker for gallery/content
        public static bool TryInvokeAnyUnlockGallery()
        {
            string[] methodTokens = { "unlockall", "unlock_gallery", "unlockgallery", "unlock", "revealall", "completegallery" };
            foreach (var t in FindTypes("gallery", "codex", "collection", "unlock"))
            {
                var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                for (int i = 0; i < methods.Length; i++)
                {
                    var mi = methods[i];
                    var n = mi.Name.ToLowerInvariant();
                    bool tokenMatch = false;
                    for (int k = 0; k < methodTokens.Length; k++)
                    {
                        if (n.IndexOf(methodTokens[k], StringComparison.Ordinal) >= 0) { tokenMatch = true; break; }
                    }
                    if (tokenMatch && mi.GetParameters().Length == 0)
                    {
                        try { mi.Invoke(null, null); return true; } catch { }
                    }
                }
            }

            // Fallback: set a few common PlayerPrefs flags used by some games
            try
            {
                PlayerPrefs.SetInt("gallery_unlocked", 1);
                PlayerPrefs.SetInt("unlock_all_gallery", 1);
                PlayerPrefs.Save();
                return true;
            }
            catch { }

            return false;
        }

        // Dump object fields/properties for display
        public static void DumpObject(object obj, Action<string> lineOut, int maxDepth = 2, int maxItems = 128)
        {
            var visited = new HashSet<object>(new ReferenceEqualityComparer());
            Dump(obj, lineOut, 0, maxDepth, ref maxItems, visited, "root");
        }

        private static void Dump(object obj, Action<string> lineOut, int depth, int maxDepth, ref int maxItems, HashSet<object> visited, string path)
        {
            if (obj == null || maxItems <= 0) return;
            if (depth > maxDepth) { lineOut(string.Format("{0}... (max depth)", Indent(depth))); return; }

            var type = obj.GetType();
            if (!type.IsValueType && !(obj is string))
            {
                if (visited.Contains(obj)) { lineOut(string.Format("{0}(circular) {1}", Indent(depth), type.Name)); return; }
                visited.Add(obj);
            }

            if (obj is string || type.IsPrimitive)
            {
                lineOut(string.Format("{0}{1}: {2}", Indent(depth), path, obj));
                maxItems--; return;
            }

            var enumerable = obj as IEnumerable;
            if (enumerable != null && !(obj is IDictionary))
            {
                int i = 0;
                foreach (var it in enumerable)
                {
                    if (maxItems <= 0) break;
                    Dump(it, lineOut, depth + 1, maxDepth, ref maxItems, visited, path + "[" + (i++) + "]");
                }
                return;
            }

            // Fields
            foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (maxItems <= 0) break;
                object val = null;
                try { val = f.GetValue(obj); } catch { continue; }
                Dump(val, lineOut, depth + 1, maxDepth, ref maxItems, visited, f.Name);
            }
            // Properties
            foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (maxItems <= 0) break;
                if (!p.CanRead) continue;
                object val = null;
                try { if (p.GetIndexParameters().Length > 0) continue; val = p.GetValue(obj, null); } catch { continue; }
                Dump(val, lineOut, depth + 1, maxDepth, ref maxItems, visited, p.Name);
            }
        }

        private static string Indent(int d) { return new string(' ', d * 2); }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            bool IEqualityComparer<object>.Equals(object x, object y) { return ReferenceEquals(x, y); }
            int IEqualityComparer<object>.GetHashCode(object obj) { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj); }
        }

        public static List<object> CollectGameRoots()
        {
            var roots = new List<object>();
            // Common singletons / managers
            var mbs = FindMonoBehaviours("player", "hero", "party", "team", "character", "farm", "barn", "field", "crop", "guild", "inventory", "trait", "status");
            for (int i = 0; i < mbs.Count; i++) roots.Add(mbs[i]);

            // Also look for types with static Instance/Singleton properties
            foreach (var t in FindTypes("manager", "controller", "service", "game", "farm", "party", "player"))
            {
                try
                {
                    var instProp = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) ??
                                   t.GetProperty("Singleton", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (!object.ReferenceEquals(instProp, null))
                    {
                        object v = null;
                        try { v = instProp.GetValue(null, null); } catch { }
                        if (!object.ReferenceEquals(v, null)) roots.Add(v);
                    }
                }
                catch { }
            }
            // Manual distinct by reference
            var uniq = new List<object>();
            var seen = new HashSet<object>(new ReferenceEqualityComparer());
            for (int i = 0; i < roots.Count; i++)
            {
                var r = roots[i];
                if (r == null) continue;
                if (seen.Add(r)) uniq.Add(r);
            }
            return uniq;
        }

        // Added helper used by GameApi: get a method regardless of visibility
        public static MethodInfo GetPrivateMethod(object instance, string name)
        {
            if (instance == null || string.IsNullOrEmpty(name)) return null;
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            try { return instance.GetType().GetMethod(name, flags); } catch { return null; }
        }
    }
}
