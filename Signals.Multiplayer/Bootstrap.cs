using System;

namespace Signals.Multiplayer
{
    /// <summary>
    /// Entry point for the multiplayer integration, called from <see cref="Signals.Game"/> via reflection.
    /// </summary>
    public static class Bootstrap
    {
        public static void Initialize(
            string modId,
            Action<string> log,
            Action<string> logVerbose,
            Action<bool, bool, bool> applyClientSettings,
            Func<bool[]> getHostSettings,
            Action setMPActive,
            Action clearMPActive,
            Action reloadSettings)
        {
            SignalNetworkManager.Initialize(modId, log, logVerbose, applyClientSettings, getHostSettings, setMPActive, clearMPActive, reloadSettings);
        }

        public static void Teardown()
        {
            SignalNetworkManager.Teardown();
        }

        /// <summary>
        /// Called by MultiplayerShim when the host saves settings. Broadcasts current values to all connected clients.
        /// No-ops when no server session is active.
        /// </summary>
        public static void BroadcastHostSettings(
            bool generateShuntingSignals,
            bool enableSignalEnforcement, 
            bool enableMisalignedTrackOccupancy
         )
        {
            SignalNetworkManager.BroadcastHostSettings(
                generateShuntingSignals, 
                enableSignalEnforcement, 
                enableMisalignedTrackOccupancy
                );
        }
    }
}
