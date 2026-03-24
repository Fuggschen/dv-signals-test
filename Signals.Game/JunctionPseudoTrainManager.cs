using System;
using System.Collections.Generic;

namespace Signals.Game
{
    /// <summary>
    /// Maintains pseudo-train occupancy for a junction by marking all non-selected
    /// outgoing branches as occupied.
    /// </summary>
    /// <remarks>
    /// This is used to make routing logic treat currently unselected branches as blocked,
    /// so only the active branch appears passable.
    /// </remarks>
    public class JunctionPseudoTrainManager
    {
        private static Dictionary<Junction, JunctionPseudoTrainManager> s_managers = new Dictionary<Junction, JunctionPseudoTrainManager>();

        /// <summary>
        /// The junction this manager tracks.
        /// </summary>
        public Junction Junction { get; }
        private readonly HashSet<RailTrack> _tracksWithPseudoTrains = new HashSet<RailTrack>();

        /// <summary>
        /// Creates a manager for a junction and subscribes to switch events.
        /// </summary>
        /// <param name="junction">Junction to track.</param>
        public JunctionPseudoTrainManager(Junction junction)
        {
            Junction = junction;
            junction.Switched += OnJunctionSwitched;
            UpdatePseudoTrains();
        }

        /// <summary>
        /// Gets an existing manager for a junction or creates one if missing.
        /// </summary>
        /// <param name="junction">Junction whose manager should be returned.</param>
        /// <returns>The manager instance for the given junction.</returns>
        public static JunctionPseudoTrainManager GetOrCreate(Junction junction)
        {
            if (!s_managers.TryGetValue(junction, out var manager))
            {
                manager = new JunctionPseudoTrainManager(junction);
                s_managers.Add(junction, manager);
            }
            return manager;
        }

        /// <summary>
        /// Checks whether a track is currently marked as pseudo-occupied by any junction manager.
        /// </summary>
        /// <param name="track">Track to check.</param>
        /// <returns><c>true</c> if any manager marks the track as pseudo-occupied; otherwise <c>false</c>.</returns>
        public static bool HasPseudoTrain(RailTrack track)
        {
            if (!SignalsMod.Settings.EnableMisalignedTrackOccupancy)
            {
                return false;
            }

            foreach (var manager in s_managers.Values)
            {
                if (manager._tracksWithPseudoTrains.Contains(track))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Refreshes pseudo-train occupancy when the junction switch changes.
        /// </summary>
        private void OnJunctionSwitched(Junction.SwitchMode mode, int branch)
        {
            UpdatePseudoTrains();
        }

        /// <summary>
        /// Rebuilds the pseudo-occupied track set so every non-selected branch is marked occupied.
        /// </summary>
        private void UpdatePseudoTrains()
        {
            _tracksWithPseudoTrains.Clear();

            int selectedBranch = Junction.selectedBranch;
            int branchCount = Junction.outBranches.Count;

            for (int i = 0; i < branchCount; i++)
            {
                // The active branch should remain passable; all others get pseudo occupancy.
                if (i == selectedBranch) continue;

                var branchTrack = Junction.outBranches[i].track;
                if (branchTrack != null)
                {
                    _tracksWithPseudoTrains.Add(branchTrack);
                }
            }
        }

        /// <summary>
        /// Clears all manager state and tracked pseudo-train occupancy across all junctions.
        /// </summary>
        public static void ClearAll()
        {
            foreach (var manager in s_managers.Values)
            {
                manager._tracksWithPseudoTrains.Clear();
            }
            s_managers.Clear();
        }
    }
}
