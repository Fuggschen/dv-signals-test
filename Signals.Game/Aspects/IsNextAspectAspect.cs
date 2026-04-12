using Signals.Common.Aspects;
using Signals.Game.Controllers;

namespace Signals.Game.Aspects
{
    internal class IsNextAspectAspect : AspectBase
    {
        private IsNextAspectAspectDefinition _fullDef;

        public IsNextAspectAspect(AspectBaseDefinition definition, BasicSignalController controller) : base(definition, controller)
        {
            _fullDef = (IsNextAspectAspectDefinition)definition;
        }

        public override bool MeetsConditions()
        {
            if (ControllerTrackInfo == null || ControllerTrackInfo.NextMainlineSignal == null) return false;

            var nextSignal = ControllerTrackInfo.NextMainlineSignal;

            var state = nextSignal.CurrentAspect;

            if (state != null && state.Id == _fullDef.NextId)
            {
                return true;
            }

            foreach (var indicator in nextSignal.AllIndicators)
            {
                if (indicator.Active && indicator.Id == _fullDef.NextId)
                    return true;
            }

            return false;
        }
    }
}
