using Signals.Common;
using Signals.Game.Railway;

namespace Signals.Game.Controllers
{
    internal class ShuntingSignalController : BasicSignalController
    {
        public TrackDirection Direction { get; private set; }
        public RailTrack Track { get; private set; }

        // Optional junction data — set after construction for branch signals.
        public Junction? Junction { get; private set; }
        public int BranchIndex { get; private set; } = -1;

        public ShuntingSignalController(SignalControllerDefinition def, RailTrack track, TrackDirection direction, SignalPlacementInfo info) :
            base(def, info)
        {
            Type = SignalType.Shunting;
            PrefabType = PrefabType.Shunting;
            Operation = SignalOperationMode.FullManual;
            Direction = direction;
            Track = track;
            Block = TrackBlock.CreateForShunting(track);
            InternalName = $"{Block.Station}-{Block.Yard}{Block.TrackNumber}{PlacementLetter}";

            ChangeToLeastRestrictive(true);
        }

        internal void SetJunctionInfo(Junction junction, int branchIndex)
        {
            Junction = junction;
            BranchIndex = branchIndex;
        }

        /// <summary>
        /// Checks if the junction is currently aligned for this signal's branch.
        /// OUT-facing signals are always considered aligned.
        /// IN-facing signals check <see cref="Junction.selectedBranch"/> against <see cref="BranchIndex"/>.
        /// </summary>
        public bool IsJunctionAligned()
        {
            if (Direction == TrackDirection.Out) return true;
            if (Junction == null) return true;
            return Junction.selectedBranch == BranchIndex;
        }
    }
}

