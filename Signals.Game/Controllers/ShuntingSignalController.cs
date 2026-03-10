using Signals.Common;

namespace Signals.Game.Controllers
{
    /// <summary>
    /// Controls a shunting signal that checks track alignment and occupancy.
    /// </summary>
    /// <remarks>
    /// Shunting signals show:
    /// - WHITE/PROCEED when the junction is aligned for their branch AND track is clear
    /// - RED/STOP when the junction is NOT aligned OR track is occupied
    /// </remarks>
    public class ShuntingSignalController : BasicSignalController
    {
        // Signals at over this distance from the camera update at a slower rate.
        private const float SlowUpdateDistanceSqr = 1500 * 1500;
        private const float SkipUpdateDistanceSqr = 5000 * 5000;
        // Maximum number of times the update can be delayed.
        private const int MaxUpdateDelay = 5;
        // Maximum distance to walk for shunting signals (in meters)
        private const float MaxWalkDistance = 150f;

        private int _updateDelay = 0;

        public Junction Junction { get; protected set; }
        public int BranchIndex { get; protected set; }
        public RailTrack StartTrack { get; protected set; }
        public TrackDirection Direction { get; protected set; }

        public override string Name => string.IsNullOrEmpty(NameOverride) 
            ? $"{Junction.junctionData.junctionIdLong}-SH{BranchIndex}-{(Direction.IsOut() ? 'O' : 'I')}" 
            : NameOverride;

        public ShuntingSignalController(
            SignalControllerDefinition def, 
            Junction junction, 
            int branchIndex,
            RailTrack startTrack,
            TrackDirection direction) : base(def)
        {
            Junction = junction;
            BranchIndex = branchIndex;
            StartTrack = startTrack;
            Direction = direction;
            Type = SignalType.Shunting;

            Junction.Switched += JunctionSwitched;
            Destroyed += (x) => Junction.Switched -= JunctionSwitched;
        }

        private void JunctionSwitched(Junction.SwitchMode mode, int branch)
        {
            if (ManualOperationOnly) return;

            // Force update when junction switches
            UpdateAspect();
            UpdateDisplays(true);
        }

        public override bool ShouldSkipUpdate()
        {
            if (base.ShouldSkipUpdate()) return true;

            var dist = GetCameraDistanceSqr();

            // If the camera is too far from the signal, skip updating.
            // If the camera is far, but the signal is relatively close, use a slowed update rate.
            if (dist > SkipUpdateDistanceSqr || (dist > SlowUpdateDistanceSqr && _updateDelay < MaxUpdateDelay))
            {
                _updateDelay++;
                return true;
            }

            _updateDelay = 0;
            return false;
        }

        public override void UpdateAspect()
        {
            // For diverging signals (OUT), walk from the junction based on selected branch
            // For merging signals (IN), walk from the fixed StartTrack
            
            if (Direction == TrackDirection.Out)
            {
                // Diverging: Walk from junction using the currently selected branch
                TrackInfo = TrackWalker.WalkUntilNextSignal(Junction, Direction, Junction.selectedBranch);
            }
            else
            {
                // Merging: Walk from the fixed track this signal is on
                TrackInfo = TrackWalker.WalkFromPosition(StartTrack, Direction, MaxWalkDistance);
            }

            base.UpdateAspect();
        }

        /// <summary>
        /// Checks if the junction is aligned for this shunting signal's branch.
        /// </summary>
        /// <remarks>
        /// For signals facing OUT (away from junction on diverging tracks), the junction is always 
        /// aligned because trains can exit to any branch. For signals facing IN (towards the junction
        /// on merging tracks), only the selected branch is aligned.
        /// </remarks>
        public bool IsJunctionAligned()
        {
            // Signals facing OUT away from junction are always aligned
            // (trains can exit to any diverging track)
            if (Direction == TrackDirection.Out)
            {
                return true;
            }

            // Signals facing IN towards junction must check if their branch is selected
            // (only one merging track can proceed through the junction)
            return Junction.selectedBranch == BranchIndex;
        }
    }
}

