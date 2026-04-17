using Signals.Common.Aspects;
using Signals.Game.Controllers;
using System.Linq;

namespace Signals.Game.Aspects
{
    public class IsNextAspectAnyAspect : AspectBase
    {
        private IsNextAspectAnyAspectDefinition _fullDef;

        public IsNextAspectAnyAspect(AspectBaseDefinition definition, BasicSignalController controller) : base(definition, controller)
        {
            _fullDef = (IsNextAspectAnyAspectDefinition)definition;
        }

        public override bool MeetsConditions()
        {
            var next = Controller.GetNextSignal();

            if (next == null) return false;

            var state = next.CurrentAspect;

            // Turned off signal can never meet conditions.
            if (state != null && _fullDef.NextIds.Contains(state.Id))
            {
                return true;
            }

            foreach (var indicator in next.AllIndicators)
            {
                if (indicator.Active && _fullDef.NextIds.Contains(indicator.Id))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
