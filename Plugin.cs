using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CloudMeadow.CreativeMode
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "dev.rovodev.cloudmeadow.creativemode";
        public const string PLUGIN_NAME = "Cloud Meadow Creative Mode";
        public const string PLUGIN_VERSION = "1.0.0";

        internal static ManualLogSource Log;
        internal static Harmony Harmony;

        // Configs
        internal static ConfigEntry<KeyCode> ToggleOverlayKey;
        internal static ConfigEntry<KeyCode> UnlockGalleryKey;
        internal static ConfigEntry<KeyCode> RefreshScanKey;

        private string _startupLogPath;

        private void Awake()
        {
            Log = Logger;
            Harmony = new Harmony(PLUGIN_GUID);

            try
            {
                var dir = System.IO.Path.Combine(BepInEx.Paths.GameRootPath, "BepInEx");
                dir = System.IO.Path.Combine(dir, "plugins");
                dir = System.IO.Path.Combine(dir, "CloudMeadowCreativeMode");
                _startupLogPath = System.IO.Path.Combine(dir, "creative_startup.log");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_startupLogPath));
                System.IO.File.WriteAllText(_startupLogPath, "=== Creative Mode Startup ===\n" + DateTime.Now.ToString("u") + "\n");
                AppendStartup("UnityVersion=" + Application.unityVersion);
                AppendStartup("CLR=" + Environment.Version);
            }
            catch { }

            ToggleOverlayKey = Config.Bind("Hotkeys", "ToggleOverlay", KeyCode.F6, "Toggle the Creative Mode overlay");
            UnlockGalleryKey = Config.Bind("Hotkeys", "UnlockGallery", KeyCode.F7, "Attempt to unlock gallery");
            RefreshScanKey = Config.Bind("Hotkeys", "RefreshScan", KeyCode.F8, "Rescan scene for game objects");

            try
            {
                gameObject.AddComponent<UIOverlay>();
                AppendStartup("UIOverlay added");
            }
            catch (Exception ex)
            {
                AppendStartup("UIOverlay add failed: " + ex);
            }

            try
            {
                gameObject.AddComponent<GameEventsListener>();
                AppendStartup("GameEventsListener added");
            }
            catch (Exception ex)
            {
                AppendStartup("GameEventsListener add failed: " + ex);
            }

            LogBuffer.Add("Creative Mode plugin initialized");

            try
            {
                Harmony.PatchAll();
                AppendStartup("Harmony patched");
            }
            catch (Exception e)
            {
                Log.LogError(string.Format("Harmony patch error: {0}", e));
                AppendStartup("Harmony patch error: " + e);
            }

            Log.LogInfo(PLUGIN_NAME + " " + PLUGIN_VERSION + " loaded");
        }

        private void AppendStartup(string line)
        {
            try { System.IO.File.AppendAllText(_startupLogPath, line + "\n"); } catch { }
        }

        private void OnDestroy()
        {
            try { if (Harmony != null) Harmony.UnpatchSelf(); } catch { }
        }
    }
}
