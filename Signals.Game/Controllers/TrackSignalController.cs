using Signals.Common;
using Signals.Game.Railway;
using System.Collections.Generic;

namespace Signals.Game.Controllers
{
    public class TrackSignalController : BasicSignalController
    {
        // Signals too far from the camera aren't updated.
        private const float SkipUpdateDistanceSqr = 1500 * 1500;

        public RailTrack StartingTrack { get; protected set; }
        public TrackDirection Direction { get; protected set; }

        /// <summary>
        /// Junctions (and their required branch indices) absorbed into this signal via the merge pass.
        /// Used so <see cref="Aspects.TrainDetectedAspect"/> can check alignment on every junction
        /// in the chain, not just the one directly behind the signal's <see cref="StartingTrack"/>.
        /// </summary>
        public List<(Junction junction, int branchIndex)> AdditionalJunctions { get; } =
            new List<(Junction junction, int branchIndex)>();

        public TrackSignalController(SignalControllerDefinition def, RailTrack starting, TrackDirection startingDirection, SignalPlacementInfo info) :
            base(def, info)
        {
            StartingTrack = starting;
            Direction = startingDirection;

            if (starting.isJunctionTrack)
            {
                var junction = starting.inJunction;
                int count = 0;

                foreach (var item in junction.outBranches)
                {
                    count++;

                    if (item.track == starting) break;
                }

                InternalName = $"{junction.junctionData.junctionIdLong}:B{count}";
            }
            else
            {
                InternalName = $"{StartingTrack.name}:{PlacementLetter}";
            }
        }

        protected override bool ShouldMoveForwards(RailTrack track)
        {
            return StartingTrack != track;
        }

        public override bool ShouldUpdate()
        {
            if (Operation.IsFullyManual()) return false;

            var dist = GetCameraDistanceSqr();

            // If the camera is too far from the signal, skip updating.
            return dist < SkipUpdateDistanceSqr;
        }

        public override void UpdateBlock()
        {
            Block = TrackBlock.CreateUntilSignal(StartingTrack, Direction, Type == SignalType.Shunting, this);
        }
    }
}
