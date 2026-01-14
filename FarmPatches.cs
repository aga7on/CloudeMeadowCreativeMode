using System;
using HarmonyLib;
using TeamNimbus.CloudMeadow.Farm;

namespace CloudMeadow.CreativeMode
{
    [HarmonyPatch(typeof(FarmSceneManager))]
    internal static class FarmPatches
    {
        // Run after farm scene manager initializes, so segments are ready
        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        private static void PostStart(FarmSceneManager __instance)
        {
            try
            {
                if (GameApi.PendingFarmLayoutRefresh)
                {
                    GameApi.TryRefreshFarmLayout(__instance);
                    GameApi.ClearPendingFarmLayoutRefresh();
                }
            }
            catch { }
        }
    }
}
