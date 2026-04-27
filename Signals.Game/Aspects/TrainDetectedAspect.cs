using Signals.Common.Aspects;
using Signals.Game.Controllers;

namespace Signals.Game.Aspects
{
    internal class TrainDetectedAspect : AspectBase
    {
        private TrainDetectedAspectDefinition _fullDef;
        private bool _initialized = false;
        private Junction? _junction;
        private int _branchIndex = -1;

        public TrainDetectedAspect(AspectBaseDefinition definition, BasicSignalController controller) : base(definition, controller)
        {
            _fullDef = (TrainDetectedAspectDefinition)definition;
        }

        // Lazy init: StartingTrack is assigned in TrackSignalController's constructor body,
        // which runs AFTER base() — so it is null when this aspect's constructor runs.
        private void Initialize()
        {
            _initialized = true;

            if (!(Controller is TrackSignalController trackController) || Controller is JunctionSignalController) return;

            var startingTrack = trackController.StartingTrack;

            if (startingTrack == null || !startingTrack.isJunctionTrack) return;

            _junction = startingTrack.inJunction;

            for (int i = 0; i < _junction.outBranches.Count; i++)
            {
                if (_junction.outBranches[i].track == startingTrack)
                {
                    _branchIndex = i;
                    break;
                }
            }

            if (_branchIndex >= 0)
            {
                _junction.Switched += (_, __) => Controller.RequestUpdate(1);
            }
            else
            {
                _junction = null;
            }
        }

        public override bool MeetsConditions()
        {
            if (!_initialized) Initialize();

            var block = ControllerTrackBlock;

            if (SignalsMod.Settings.EnableMisalignedTrackOccupancy)
            {
                // Check the primary junction (from the signal's own StartingTrack).
                if (_junction != null && _junction.selectedBranch != _branchIndex)
                {
                    return true;
                }

                // Walk every track in the block and check each junction for alignment.
                // When signals are merged because junctions are tightly spaced, the block
                // may span multiple junctions between the exit and entrance signals.
                // Each junction must be aligned to the branch the block's route follows.
                if (block != null)
                {
                    foreach (var track in block.Tracks)
                    {
                        if (!track.isJunctionTrack) continue;

                        // Skip the primary junction — already checked above.
                        var junction = track.inJunction;
                        if (junction == _junction) continue;

                        for (int i = 0; i < junction.outBranches.Count; i++)
                        {
                            if (junction.outBranches[i].track == track)
                            {
                                if (junction.selectedBranch != i)
                                {
                                    return true;
                                }

                                break;
                            }
                        }
                    }
                }
            }

            return block != null && block.IsOccupied(_fullDef.CrossingCheckMode);
        }
    }
}