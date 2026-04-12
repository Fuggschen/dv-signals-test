using Signals.API;
using Signals.Common.Aspects;
using Signals.Game.Controllers;

namespace Signals.Game.Aspects
{
    internal class SignalModeAspect : AspectBase
    {
        private readonly SignalModeAspectDefinition _fullDef;

        public SignalModeAspect(AspectBaseDefinition definition, BasicSignalController controller) : base(definition, controller)
        {
            _fullDef = (SignalModeAspectDefinition)definition;
        }

        public override bool MeetsConditions()
        {
            var targetMode = _fullDef.ActiveOnMode == SignalModeCondition.Manual
                ? SignalMode.Manual
                : SignalMode.Automatic;

            return Controller.Mode == targetMode;
        }
    }
}
