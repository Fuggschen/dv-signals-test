using System;

namespace Signals.Multiplayer
{
    /// <summary>
    /// Entry point for the multiplayer integration, called from <see cref="Signals.Game"/> via reflection.
    /// </summary>
    public static class Bootstrap
    {
        public static void Initialize(string modId, Action<string> log, Action<string> logVerbose)
        {
            SignalNetworkManager.Initialize(modId, log, logVerbose);
        }

        public static void Teardown()
        {
            SignalNetworkManager.Teardown();
        }
    }
}
