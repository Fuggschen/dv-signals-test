using System;
using System.Xml.Serialization;
using UnityModManagerNet;

namespace Signals.Game
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        // Disabled since we don't support using custom packs in ops
        //[Draw("Custom Pack", Tooltip = "The mod ID of a custom signals pack")]
        public string CustomPack = string.Empty;

        // Disabled since the provided pack has no shunting signals
        //[Draw("Generate Shunting Signals")]
        public bool GenerateShuntingSignals = false;

        [Draw("Use Verbose Logging", Tooltip = "Logs a lot more information\n" +
            "Useful if you are experiencing bugs")]
        public bool UseVerboseLogging = false;

        [Draw("Enable Signal Enforcement", Tooltip = "Plays an alarm when a train passes a signal at danger (DisallowPassing aspect).\nEnable the traction-type settings below to also apply emergency brakes.")]
        public bool EnableSignalEnforcement = false;

        [Draw("(Diesel) Enable Automatic Emergency Brake",
            Tooltip = "Diesel and electric locomotives will receive emergency brakes when passing a signal at danger.\nRequires Signal Enforcement to be enabled.",
            VisibleOn = "EnableSignalEnforcement|true")]
        public bool EnableDieselEnforcement = false;

        [Draw("(Steam) Enable Automatic Emergency Brake",
            Tooltip = "Steam locomotives (S060 and S282) will receive emergency brakes when passing a signal at danger.\nRequires Signal Enforcement to be enabled.",
            VisibleOn = "EnableSignalEnforcement|true")]
        public bool EnableSteamEnforcement = false;

        // Drawn manually in SignalsMod.DrawGUI so it remains editable even in multiplayer.
        public float IndusiVolume = 1f;
        
        [Draw("Auto-revert Manual Signals", Tooltip = "When a train passes a signal in Manual mode, it automatically reverts to Automatic mode")]
        public bool AutoRevertManualSignals = true;

        [Draw("Enable Misaligned Track Occupancy", Tooltip = "Marks signals on unselected junction branches as occupied, blocking routes through misaligned switches")]
        public bool EnableMisalignedTrackOccupancy = false;

        /// <summary>Set to true on clients while a multiplayer session is active. Blocks local disk saves.</summary>
        [XmlIgnore]
        public bool MPActive = false;

        /// <summary>Fired after settings are saved (host) or applied from the host (client). Use to react to settings changes at runtime.</summary>
        [XmlIgnore]
        public Action<Settings>? OnSettingsSaved;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            if (!MPActive)
            {
                Save(this, modEntry);
            }
            OnSettingsSaved?.Invoke(this);
        }

        public void OnChange() { }
    }
}
