using System;
using System.Collections.Generic;

namespace Signals.API
{
    /// <summary>
    /// Public API for querying and controlling signals.
    /// </summary>
    public interface ISignalsAPI
    {
        /// <summary>
        /// Returns a snapshot of all signals currently registered in the world.
        /// </summary>
        IReadOnlyList<SignalState> GetAllSignals();

        /// <summary>
        /// Returns a snapshot of a single signal by its ID.
        /// </summary>
        /// <param name="signalId">The unique name of the signal.</param>
        /// <returns>The signal state, or <see langword="null"/> if no signal with this ID exists.</returns>
        SignalState? GetSignal(string signalId);

        /// <summary>
        /// Sets the aspect of a signal by its ID. The signal will enter <see cref="SignalMode.Manual"/> mode
        /// and keep this aspect until the mode is changed back to <see cref="SignalMode.Automatic"/>.
        /// </summary>
        /// <param name="signalId">The unique name of the signal.</param>
        /// <param name="aspectId">The ID of the aspect to set (e.g. "OPEN", "STOP").</param>
        /// <returns><see langword="true"/> if the aspect was set successfully, <see langword="false"/> otherwise.</returns>
        bool SetSignalAspect(string signalId, string aspectId);

        /// <summary>
        /// Switches the operating mode of a signal. When switching to <see cref="SignalMode.Automatic"/>,
        /// the signal will immediately re-evaluate its aspect based on current conditions.
        /// </summary>
        /// <param name="signalId">The unique name of the signal.</param>
        /// <param name="mode">The mode to switch to.</param>
        /// <returns><see langword="true"/> if the mode was changed, <see langword="false"/> otherwise.</returns>
        bool SetSignalMode(string signalId, SignalMode mode);

        /// <summary>
        /// Turns off a signal (no active aspect). The signal enters <see cref="SignalMode.Manual"/> mode.
        /// </summary>
        /// <param name="signalId">The unique name of the signal.</param>
        /// <returns><see langword="true"/> if the signal was turned off, <see langword="false"/> otherwise.</returns>
        bool TurnOffSignal(string signalId);

        /// <summary>
        /// Fired when any signal's aspect changes, whether by automatic logic or manual override.
        /// The <see cref="SignalState"/> snapshot reflects the state after the change.
        /// </summary>
        event Action<SignalState>? SignalAspectChanged;

        /// <summary>
        /// Fired when a signal's operating mode changes.
        /// Parameters are the signal ID and the new mode.
        /// </summary>
        event Action<string, SignalMode>? SignalModeChanged;
    }
}
