using System;
using HarmonyLib;
using TeamNimbus.CloudMeadow.Controllers;
using UnityEngine;

namespace CloudMeadow.CreativeMode
{
    // Patch PlayerController to multiply speed used for UpdateVelocity
    [HarmonyPatch(typeof(PlayerController))]
    internal static class MovementPatches
    {
        // The method UpdateVelocity(Vector2, float) is called with speed computed from run/walk.
        // We postfix to scale the rigidbody velocity after the call.
        [HarmonyPostfix]
        [HarmonyPatch("UpdateVelocity", new Type[] { typeof(Vector2), typeof(float) })]
        private static void Postfix_UpdateVelocity(PlayerController __instance)
        {
            try
            {
                var rbField = typeof(PlayerController).GetField("_rigidBody2D", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var rb = rbField != null ? rbField.GetValue(__instance) as Rigidbody2D : null;
                if (rb != null)
                {
                    if (GameApi.SpeedMultiplier != 1f)
                    {
                        rb.velocity = rb.velocity * GameApi.SpeedMultiplier;
                    }
                }
            }
            catch { }
        }
    }
}
