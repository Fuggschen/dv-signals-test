﻿using Signals.Game.Controllers;
using System.Collections.Generic;
using System.Linq;

using Branch = Junction.Branch;

namespace Signals.Game
{
    public enum TrackDirection
    {
        Out,
        In
    }

    /// <summary>
    /// Helper class to traverse tracks.
    /// </summary>
    public static class TrackWalker
    {
        public const int MaxDepth = 64;

        /// <summary>
        /// Returns all tracks after a signal until another signal is found.
        /// </summary>
        /// <param name="controller">The <see cref="SignalController"/> from where to start.</param>
        public static TrackInfo WalkUntilNextSignal(JunctionSignalController controller)
        {
            return WalkUntilNextSignal(controller.Junction, controller.Direction);
        }

        /// <summary>
        /// Returns all tracks after a junction until a signal is found.
        /// </summary>
        /// <param name="from">The <see cref="Junction"/> where to start.</param>
        /// <param name="direction">The search direction. <see langword="true"/> for the outbound tracks, <see langword="false"/> for the inbound track.</param>
        /// <remarks>Uses the currently selected branch.</remarks>
        public static TrackInfo WalkUntilNextSignal(Junction from, TrackDirection direction)
        {
            return WalkUntilNextSignal(from, direction, from.selectedBranch);
        }

        /// <summary>
        /// Returns all tracks after a junction until a signal is found.
        /// </summary>
        /// <param name="from">The <see cref="Junction"/> where to start.</param>
        /// <param name="direction">The search direction. <see langword="true"/> for the outbound tracks, <see langword="false"/> for the inbound track.</param>
        /// <param name="branch">The junction branch to follow.</param>
        /// <returns></returns>
        public static TrackInfo WalkUntilNextSignal(Junction from, TrackDirection direction, int branch)
        {
            var track = direction.IsOut() ? from.outBranches[branch].track : from.inBranch.track;
            return WalkUntilNextSignal(track, track.inJunction == from ? TrackDirection.Out : TrackDirection.In);
        }

        /// <summary>
        /// Performs a search starting from <paramref name="track"/> in <paramref name="direction"/> until the next junction signal is found.
        /// </summary>
        /// <param name="track">The track to start on.</param>
        /// <param name="direction">The direction to search in. <see langword="true"/> for the out branch, <see langword="false"/> for the in branch.</param>
        /// <returns></returns>
        public static TrackInfo WalkUntilNextSignal(RailTrack track, TrackDirection direction)
        {
            int depth = 0;
            HashSet<RailTrack> visited = new HashSet<RailTrack>();
            List<RailTrack> ordered = new List<RailTrack>();
            JunctionSignalController? mainlineSignal = null;
            JunctionSignalController? shuntingSignal = null;
            Junction? nextJunction = null;

            // Keep looping until a certain depth is reached, the track exists and the track has not been visited yet.
            while (depth++ < MaxDepth && track != null && !visited.Contains(track))
            {
                visited.Add(track);
                ordered.Add(track);

                Junction? junction = direction.IsOut() ? track.outJunction : track.inJunction;
                Branch? branch;

                if (junction != null)
                {
                    bool junctionDir = junction.inBranch.track == track;

                    if (nextJunction == null)
                    {
                        nextJunction = junction;
                    }

                    // If the junction has a signal for the current direction, stop the loop.
                    if (SignalManager.Instance.TryGetSignals(junction, out var signals))
                    {
                        mainlineSignal = signals.GetSignal(junctionDir ? TrackDirection.Out : TrackDirection.In);

                        if (mainlineSignal != null)
                        {
                            switch (mainlineSignal.Type)
                            {
                                case SignalType.Mainline:
                                case SignalType.IntoYard:
                                    goto ExitLoop;
                                case SignalType.Shunting:
                                    shuntingSignal ??= mainlineSignal;
                                    goto default;
                                default:
                                    mainlineSignal = null;
                                    break;
                            }
                        }
                    }

                    // Otherwise, take the branch from the junction.
                    // Out branch if the current track is the in branch,
                    // and vice versa.
                    branch = junctionDir ?
                        junction.outBranches[junction.selectedBranch] :
                        junction.inBranch;
                }
                else
                {
                    // If there's no junction just use the track branch directly.
                    branch = direction.IsOut() ? track.outBranch : track.inBranch;
                }

                // No branch means we have no track to go, stop looping.
                if (branch == null || branch.track == null)
                {
                    break;
                }

                // Check if the current track is the next track of the next branch.
                if (ContainsTrack(track, branch, direction))
                {
                    direction = direction.Flipped();
                }

                track = branch.track;
            }

            ExitLoop:

            return new TrackInfo(ordered, direction, mainlineSignal, shuntingSignal, nextJunction);
        }

        private static bool ContainsTrack(RailTrack from, Branch branch, TrackDirection direction)
        {
            var nextBranch = direction.IsOut() ? branch.track.outBranch : branch.track.inBranch;

            // If the next visited track was going to be track we came from, we must go the other way.
            if (nextBranch != null && nextBranch.track == from)
            {
                return true;
            }

            var nextJuntion = direction.IsOut() ? branch.track.outJunction : branch.track.inJunction;

            // Check the junction if the branch was null.
            if (nextJuntion != null)
            {
                if (nextJuntion.inBranch != null && nextJuntion.inBranch.track == from)
                {
                    return true;
                }

                return nextJuntion.outBranches.Any(x => x.track == from);
            }

            return false;
        }

        public static IEnumerable<RailTrack> Walk(JunctionSignalController controller)
        {
            return Walk(controller.Junction, controller.Direction);
        }

        public static IEnumerable<RailTrack> Walk(Junction from, TrackDirection direction)
        {
            return Walk(from, direction, from.selectedBranch);
        }

        public static IEnumerable<RailTrack> Walk(Junction from, TrackDirection direction, int branch)
        {
            var track = direction.IsOut() ? from.outBranches[branch].track : from.inBranch.track;
            return Walk(track, track.inJunction == from ? TrackDirection.Out : TrackDirection.In);
        }

        public static IEnumerable<RailTrack> Walk(RailTrack track, TrackDirection direction)
        {
            int depth = 0;
            HashSet<RailTrack> visited = new HashSet<RailTrack>();

            // Keep looping until a certain depth is reached, the track exists and the track has not been visited yet.
            while (depth++ < MaxDepth && track != null && !visited.Contains(track))
            {
                visited.Add(track);

                Junction? junction = direction.IsOut() ? track.outJunction : track.inJunction;
                Branch? branch;

                if (junction != null)
                {
                    branch = junction.inBranch.track == track ?
                        junction.outBranches[junction.selectedBranch] :
                        junction.inBranch;
                }
                else
                {
                    // If there's no junction just use the track branch directly.
                    branch = direction.IsOut() ? track.outBranch : track.inBranch;
                }

                // No branch means we have no track to go, stop looping.
                if (branch == null || branch.track == null)
                {
                    break;
                }

                // Check if the current track is the next track of the next branch.
                if (ContainsTrack(track, branch, direction))
                {
                    direction = direction.Flipped();
                }

                track = branch.track;
                yield return track;
            }
        }

        /// <summary>
        /// Walks track from a specific position with a distance limit (for shunting signals).
        /// </summary>
        /// <param name="startTrack">The track to start walking from</param>
        /// <param name="direction">The direction to walk</param>
        /// <param name="maxDistance">Maximum distance to walk in meters</param>
        /// <returns>TrackInfo containing all tracks within the distance limit</returns>
        public static TrackInfo WalkFromPosition(RailTrack startTrack, TrackDirection direction, float maxDistance)
        {
            int depth = 0;
            HashSet<RailTrack> visited = new HashSet<RailTrack>();
            List<RailTrack> ordered = new List<RailTrack>();
            BasicSignalController? mainlineSignal = null;
            BasicSignalController? shuntingSignal = null;
            Junction? nextJunction = null;
            float distanceWalked = 0f;

            // Start by getting the first track ahead (not the starting track itself)
            Junction? firstJunction = direction.IsOut() ? startTrack.outJunction : startTrack.inJunction;
            Branch? firstBranch;
            
            if (firstJunction != null)
            {
                bool junctionDir = firstJunction.inBranch.track == startTrack;
                
                // Check if there's a signal at this junction before walking
                if (SignalManager.Instance.TryGetSignals(firstJunction, out var signals))
                {
                    var foundSignal = signals.GetSignal(junctionDir ? TrackDirection.Out : TrackDirection.In);
                    
                    if (foundSignal != null)
                    {
                        // Found a signal immediately - don't walk any tracks
                        if (foundSignal.Type == SignalType.Mainline || foundSignal.Type == SignalType.IntoYard)
                        {
                            mainlineSignal = foundSignal;
                        }
                        else if (foundSignal.Type == SignalType.Shunting)
                        {
                            shuntingSignal = foundSignal;
                        }
                        
                        // Return empty track list since we hit a signal immediately
                        return new TrackInfo(ordered, direction, mainlineSignal, shuntingSignal, firstJunction);
                    }
                }
                
                nextJunction = firstJunction;
                firstBranch = junctionDir ?
                    firstJunction.outBranches[firstJunction.selectedBranch] :
                    firstJunction.inBranch;
            }
            else
            {
                firstBranch = direction.IsOut() ? startTrack.outBranch : startTrack.inBranch;
            }
            
            if (firstBranch == null || firstBranch.track == null)
            {
                // Dead end - no tracks ahead
                return new TrackInfo(ordered, direction, null, null, nextJunction);
            }
            
            RailTrack? track = firstBranch.track;
            TrackDirection walkDirection = direction;
            
            // Check if we need to flip direction
            if (firstJunction != null && ContainsTrack(startTrack, firstBranch, direction))
            {
                walkDirection = walkDirection.Flipped();
            }

            // Now walk the tracks ahead
            while (depth++ < MaxDepth && track != null && !visited.Contains(track))
            {
                float trackLength = (float)track.GetLength();
                
                // Check if adding this track would exceed max distance
                if (distanceWalked + trackLength > maxDistance)
                {
                    break;
                }
                
                visited.Add(track);
                ordered.Add(track);
                distanceWalked += trackLength;

                Junction? junction = walkDirection.IsOut() ? track.outJunction : track.inJunction;
                Branch? branch;

                if (junction != null)
                {
                    bool junctionDir = junction.inBranch.track == track;

                    if (nextJunction == null)
                    {
                        nextJunction = junction;
                    }

                    // If the junction has a signal for the current direction, check it.
                    if (SignalManager.Instance.TryGetSignals(junction, out var signals))
                    {
                        var foundSignal = signals.GetSignal(junctionDir ? TrackDirection.Out : TrackDirection.In);

                        if (foundSignal != null)
                        {
                            switch (foundSignal.Type)
                            {
                                case SignalType.Mainline:
                                case SignalType.IntoYard:
                                    mainlineSignal = foundSignal;
                                    goto ExitLoop;
                                    
                                case SignalType.Shunting:
                                    shuntingSignal = foundSignal;
                                    goto ExitLoop;
                            }
                        }
                    }

                    // Take the branch from the junction.
                    branch = junctionDir ?
                        junction.outBranches[junction.selectedBranch] :
                        junction.inBranch;
                }
                else
                {
                    // If there's no junction just use the track branch directly.
                    branch = walkDirection.IsOut() ? track.outBranch : track.inBranch;
                }

                // No branch means we have no track to go, stop looping.
                if (branch == null || branch.track == null)
                {
                    break;
                }

                // Check if the current track is the next track of the next branch.
                if (ContainsTrack(track, branch, walkDirection))
                {
                    walkDirection = walkDirection.Flipped();
                }

                track = branch.track;
            }

            ExitLoop:

            return new TrackInfo(ordered, walkDirection, mainlineSignal, shuntingSignal, nextJunction);
        }
    }
}
