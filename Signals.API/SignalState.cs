using UnityEngine;

namespace Signals.API
{
    /// <summary>
    /// An immutable snapshot of a signal's current state.
    /// </summary>
    public sealed class SignalState
    {
        /// <summary>
        /// The unique name of this signal (e.g. "S-0370-MF-T").
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

        /// <summary>
        /// The type/role of this signal (Mainline, Shunting, IntoYard, etc.).
        /// </summary>
        public SignalType Type { get; }

        /// <summary>
        /// The direction the signal faces relative to its junction,
        /// or <see cref="SignalDirection.None"/> if the signal is not associated with a junction.
        /// </summary>
        public SignalDirection Direction { get; }

        /// <summary>
        /// The junction ID this signal is associated with (e.g. "ST-J-01"),
        /// or <see langword="null"/> if the signal is not a junction signal.
        /// </summary>
        public string? JunctionId { get; }

        /// <summary>
        /// The currently selected branch index (0-based) of the associated junction,
        /// or <see langword="null"/> if the signal is not a junction signal.
        /// </summary>
        public int? SelectedBranch { get; }

        /// <summary>
        /// The yard/station name of the next track (e.g. "SteelMill"),
        /// or <see langword="null"/> if unavailable.
        /// </summary>
        public string? YardId { get; }

        /// <summary>
        /// The track identifier including type (e.g. "M01"),
        /// or <see langword="null"/> if unavailable.
        /// </summary>
        public string? TrackId { get; }

        public SignalState(
            string id,
            Vector3 position,
            string? currentAspectId,
            SignalMode mode,
            SignalType type,
            SignalDirection direction,
            string? junctionId,
            int? selectedBranch,
            string? yardId,
            string? trackId)
        {
            Id = id;
            Position = position;
            CurrentAspectId = currentAspectId;
            Mode = mode;
            Type = type;
            Direction = direction;
            JunctionId = junctionId;
            SelectedBranch = selectedBranch;
            YardId = yardId;
            TrackId = trackId;
        }
    }
}
