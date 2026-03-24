using System;
using System.Xml.Serialization;
using UnityModManagerNet;

namespace Signals.Game
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("Custom Pack", Tooltip = "The mod ID of a custom signals pack")]
        public string CustomPack = string.Empty;
        [Draw("Generate Shunting Signals")]
        public bool GenerateShuntingSignals = false;
        [Draw("Use Verbose Logging", Tooltip = "Logs a lot more information\n" +
            "Useful if you are experiencing bugs")]
        public bool UseVerboseLogging = false;
        // Disabled for now until there is a good way to detect trains passing signals at danger without causing false positives
        //[Draw("Enable Signal Enforcement", Tooltip = "Applies emergency brakes when a train passes a signal at danger (DisallowPassing aspect)")]
        public bool EnableSignalEnforcement = false;
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
            Save(this, modEntry);
        }

        public void OnChange() { }
    }
}
