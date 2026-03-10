using System;
using System.Collections.Generic;

namespace Signals.API
{
    /// <summary>
    /// Static entry point for the Signals public API.
    /// </summary>
    /// <remarks>
    /// All methods must be called from the Unity main thread.
    /// <see cref="Instance"/> is <see langword="null"/> until the Signals mod has finished loading signals.
    /// </remarks>
    public static class SignalsAPI
    {
        private static ISignalsAPI? _instance;

        /// <summary>
        /// The API instance, or <see langword="null"/> if not yet loaded.
        /// </summary>
        public static ISignalsAPI? Instance => _instance;

        /// <summary>
        /// Whether the Signals API is loaded and ready to use.
        /// </summary>
        public static bool IsLoaded => _instance != null;

        /// <summary>
        /// Fired when the API becomes available.
        /// </summary>
        public static event Action? Loaded;

        /// <summary>
        /// Fired when the API is being unloaded.
        /// </summary>
        public static event Action? Unloaded;

        internal static void Register(ISignalsAPI api)
        {
            _instance = api;
            Loaded?.Invoke();
        }

        internal static void Unregister()
        {
            _instance = null;
            Unloaded?.Invoke();
        }

        /// <summary>
        /// Returns a snapshot of all registered signals.
        /// </summary>
        public static IReadOnlyList<SignalState>? GetAllSignals() => _instance?.GetAllSignals();

        /// <summary>
        /// Returns a snapshot of a single signal by its ID.
        /// </summary>
        public static SignalState? GetSignal(string signalId) => _instance?.GetSignal(signalId);

        /// <summary>
        /// Sets the aspect of a signal and enters Manual mode.
        /// </summary>
        public static bool SetSignalAspect(string signalId, string aspectId) =>
            _instance?.SetSignalAspect(signalId, aspectId) ?? false;

        /// <summary>
        /// Switches the operating mode of a signal.
        /// </summary>
        public static bool SetSignalMode(string signalId, SignalMode mode) =>
            _instance?.SetSignalMode(signalId, mode) ?? false;

        /// <summary>
        /// Turns off a signal (enters Manual mode).
        /// </summary>
        public static bool TurnOffSignal(string signalId) =>
            _instance?.TurnOffSignal(signalId) ?? false;
    }
}
