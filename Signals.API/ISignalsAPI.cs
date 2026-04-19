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
        /// Checks whether the given track has any trains physically on it.
        /// This only detects real occupancy (bogies on the rail), not virtual/pseudo-occupancy
        /// used internally by the signal system.
        /// </summary>
        /// <param name="track">The track to check.</param>
        /// <returns><see langword="true"/> if at least one train bogie is on the track.</returns>
        bool IsTrackOccupied(RailTrack track);

        /// <summary>
        /// Reserves a signal's tracks for the specified duration, blocking other signals from also being reserved on those tracks.
        /// </summary>
        /// <param name="signalId">The unique name of the signal.</param>
        /// <param name="duration">How long the reservation lasts, in seconds. Must be greater than 0.</param>
        /// <returns><see langword="true"/> if the reservation was made successfully; <see langword="false"/> if the signal was not found, the duration is invalid, the API is not loaded, or another signal already holds a conflicting reservation.</returns>
        bool ReserveSignal(string signalId, float duration);

        /// <summary>
        /// Immediately clears any active track reservation belonging to the given signal.
        /// </summary>
        /// <param name="signalId">The unique name of the signal.</param>
        /// <returns><see langword="true"/> if the signal was found; <see langword="false"/> if the signal does not exist or the API is not loaded.</returns>
        bool ClearSignalReservation(string signalId);

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
