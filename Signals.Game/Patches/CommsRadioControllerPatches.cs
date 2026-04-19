using DV;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Signals.Game.Patches
{
    [HarmonyPatch(typeof(CommsRadioController))]
    internal static class CommsRadioControllerPatches
    {
        // Tracks every (controller, reserver) pair created so the setting can add/remove them at runtime.
        private static readonly List<(CommsRadioController Controller, CommsRadioSignalReserver Reserver)> s_instances
            = new List<(CommsRadioController, CommsRadioSignalReserver)>();

        private static FieldInfo? s_allModesField;

        [HarmonyPatch("Awake"), HarmonyPostfix]
        private static void AwakePostfix(CommsRadioController __instance)
        {
            // Create the object as inactive to prevent Awake() from running too early.
            var go = new GameObject(nameof(CommsRadioSignalReserver));
            go.transform.parent = __instance.transform;
            go.SetActive(false);
            var mode = go.AddComponent<CommsRadioSignalReserver>();
            mode.Controller = __instance;
            go.SetActive(true);

            s_instances.Add((__instance, mode));

            // Only inject into the mode list if the setting is currently enabled.
            if (SignalsMod.Settings.ReserveOverRemote)
            {
                AddToModes(__instance, mode);
            }
        }

        /// <summary>
        /// Called by the <see cref="Settings.OnSettingsSaved"/> subscriber in <see cref="SignalsMod"/>
        /// to show or hide the reservation mode across all live comms radio controllers.
        /// </summary>
        internal static void SetReserverEnabled(bool enabled)
        {
            // Prune stale entries (destroyed controllers).
            s_instances.RemoveAll(x => x.Controller == null);

            foreach (var (controller, reserver) in s_instances)
            {
                if (enabled)
                {
                    AddToModes(controller, reserver);
                }
                else
                {
                    // Let the reserver clean up its UI state before removal.
                    reserver.Disable();
                    RemoveFromModes(controller, reserver);
                }
            }
        }

        private static void AddToModes(CommsRadioController controller, CommsRadioSignalReserver reserver)
        {
            var modes = GetModes(controller);
            if (!modes.Contains(reserver))
            {
                modes.Add(reserver);
            }
        }

        private static void RemoveFromModes(CommsRadioController controller, CommsRadioSignalReserver reserver)
        {
            GetModes(controller)?.Remove(reserver);
        }

        private static List<ICommsRadioMode> GetModes(CommsRadioController controller)
        {
            s_allModesField ??= typeof(CommsRadioController).GetField(
                "allModes", BindingFlags.NonPublic | BindingFlags.Instance);

            return (List<ICommsRadioMode>)s_allModesField!.GetValue(controller);
        }
    }
}
