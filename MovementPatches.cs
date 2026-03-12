using HarmonyLib;
using TeamNimbus.CloudMeadow.Controllers;

namespace CloudMeadow.CreativeMode
{
    [HarmonyPatch(typeof(PlayerController))]
    internal static class MovementPatches
    {
        // Better than scaling Rigidbody velocity afterwards:
        // scale the source speed parameter directly before the game applies it.
        [HarmonyPrefix]
        [HarmonyPatch("UpdateVelocity")]
        private static void Prefix_UpdateVelocity(ref float speed)
        {
            try
            {
                float mult = GameApi.SpeedMultiplier;
                if (mult != 1f)
                {
                    speed *= mult;
                }
            }
            catch { }
        }
    }
}
