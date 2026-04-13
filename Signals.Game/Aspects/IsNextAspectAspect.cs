using Signals.Common.Aspects;
using Signals.Game.Controllers;

namespace Signals.Game.Aspects
{
    public class IsNextAspectAspect : AspectBase
    {
        private IsNextAspectAspectDefinition _fullDef;

        public IsNextAspectAspect(AspectBaseDefinition definition, BasicSignalController controller) : base(definition, controller)
        {
            _fullDef = (IsNextAspectAspectDefinition)definition;
        }

        public override bool MeetsConditions()
        {
            var next = Controller.GetNextSignal();

            if (next == null) return false;

            var state = next.CurrentAspect;

            if (state != null && state.Id == _fullDef.NextId)
            {
                return true;
            }

            foreach (var indicator in next.AllIndicators)
            {
                if (indicator.Active && indicator.Id == _fullDef.NextId)
                    return true;
            }

            return false;
        }
    }
}

