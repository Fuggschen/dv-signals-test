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

            if (!(Controller is TrackSignalController trackController)) return;

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

            if (_junction != null && SignalsMod.Settings.EnableMisalignedTrackOccupancy &&
                _junction.selectedBranch != _branchIndex)
            {
                return true;
            }

            var block = ControllerTrackBlock;

            return block != null && block.IsOccupied(_fullDef.CrossingCheckMode);
        }
    }
}
