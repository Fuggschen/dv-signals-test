using UnityEngine;

namespace Signals.API
{
    /// <summary>
    /// An immutable snapshot of a signal's current state.
    /// </summary>
    public sealed class SignalState
    {
        /// <summary>
        /// The unique name of this signal (e.g. "J-DERAIL-001-T").
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The world position of the signal.
        /// </summary>
        public Vector3 Position { get; }

        /// <summary>
        /// The ID of the currently active aspect (e.g. "OPEN", "STOP"), or <see langword="null"/> if the signal is off.
        /// </summary>
        public string? CurrentAspectId { get; }

        /// <summary>
        /// Whether the signal is currently on (has an active aspect).
        /// </summary>
        public bool IsOn => CurrentAspectId != null;

        /// <summary>
        /// The current operating mode of the signal.
        /// </summary>
        public SignalMode Mode { get; }

        public SignalState(string id, Vector3 position, string? currentAspectId, SignalMode mode)
        {
            Id = id;
            Position = position;
            CurrentAspectId = currentAspectId;
            Mode = mode;
        }
    }
}
