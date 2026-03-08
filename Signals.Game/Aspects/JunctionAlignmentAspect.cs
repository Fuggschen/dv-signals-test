using Signals.Common.Aspects;
using Signals.Game.Controllers;

namespace Signals.Game.Aspects
{
    /// <summary>
    /// Aspect implementation for junction alignment checking on shunting signals.
    /// </summary>
    internal class JunctionAlignmentAspect : AspectBase
    {
        private JunctionAlignmentAspectDefinition _fullDef;
        private ShuntingSignalController? _shuntingController;

        public JunctionAlignmentAspect(AspectBaseDefinition definition, BasicSignalController controller) : base(definition, controller)
        {
            _fullDef = (JunctionAlignmentAspectDefinition)definition;
            _shuntingController = controller as ShuntingSignalController;
        }

        public override bool MeetsConditions()
        {
            if (_shuntingController == null) return false;

            bool isAligned = _shuntingController.IsJunctionAligned();
            
            return _fullDef.ActiveWhenAligned ? isAligned : !isAligned;
        }
    }
}

