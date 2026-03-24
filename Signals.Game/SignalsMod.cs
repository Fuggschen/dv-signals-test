using UnityModManagerNet;
using UnityEngine;

namespace Signals.Game
{
    public static class SignalsMod
    {
        public const string Guid = "dv-signals";

        public static UnityModManager.ModEntry Instance { get; private set; } = null!;
        public static Settings Settings { get; private set; } = null!;

        // Unity Mod Manager Wiki: https://wiki.nexusmods.com/index.php/Category:Unity_Mod_Manager
        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            Instance = modEntry;
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            SignalManager.LoadSignals(modEntry);

            if (SignalManager.DefaultPack == null)
            {
                Error("Failed to load default pack, mod won't load!");
                return false;
            }

            Instance.OnGUI += DrawGUI;
            Instance.OnSaveGUI += Settings.Save;
            Instance.OnUnload += Unload;

#if DEBUG
            Instance.OnGUI += DebugPanel.Draw;
#endif

            ScanMods();

            UnityModManager.toggleModsListen += HandleModToggled;
            WorldStreamingInit.LoadingStatusChanged += SignalManager.CheckStartCreation;

            MultiplayerShim.Initialize();

            return true;
        }

        private static bool Unload(UnityModManager.ModEntry modEntry)
        {
            MultiplayerShim.Teardown();

#if DEBUG
            DebugPanel.Cleanup();
            Instance.OnGUI -= DebugPanel.Draw;
#endif
            UnityModManager.toggleModsListen -= HandleModToggled;
            WorldStreamingInit.LoadingStatusChanged -= SignalManager.CheckStartCreation;
            SignalManager.InstalledPacks.Clear();
            SignalManager.DefaultPack = null!;

            return true;
        }

        private static void HandleModToggled(UnityModManager.ModEntry modEntry, bool newState)
        {
            if (newState)
            {
                SignalManager.LoadSignals(modEntry);
            }
            else
            {
                SignalManager.UnloadSignals(modEntry);
            }
        }

        private static void ScanMods()
        {
            foreach (var mod in UnityModManager.modEntries)
            {
                if (mod.Active)
                {
                    SignalManager.LoadSignals(mod);
                }
            }
        }

        public static void Log(string message)
        {
            Instance.Logger.Log(message);
        }

        public static void LogVerbose(string message)
        {
            if (Settings.UseVerboseLogging)
            {
                Instance.Logger.Log(message);
            }
        }

        public static void Warning(string message)
        {
            Instance.Logger.Warning(message);
        }

        public static void Error(string message)
        {
            Instance.Logger.Error(message);
        }

        public static void ReloadSettings()
        {
            var loaded = UnityModManager.ModSettings.Load<Settings>(Instance);
            // Copy values into the existing object to preserve event subscriptions.
            Settings.CustomPack = loaded.CustomPack;
            Settings.GenerateShuntingSignals = loaded.GenerateShuntingSignals;
            Settings.UseVerboseLogging = loaded.UseVerboseLogging;
            Settings.EnableSignalEnforcement = loaded.EnableSignalEnforcement;
            Settings.EnableMisalignedTrackOccupancy = loaded.EnableMisalignedTrackOccupancy;
        }

        private static void DrawGUI(UnityModManager.ModEntry entry)
        {
            if (Settings.MPActive)
            {
                GUILayout.Label("<color=red>Settings are controlled by the host during multiplayer. Local changes will not be saved.</color>");
            }

            GUI.enabled = !Settings.MPActive;
            Settings.Draw(entry);
            GUI.enabled = true;
        }
    }
}
