using Signals.Common.Aspects;
using Signals.Game.Controllers;

namespace Signals.Game.Aspects
{
    internal class JunctionBranchAspect : AspectBase
    {
        private JunctionBranchAspectDefinition _fullDef;
        private JunctionSignalController? _junctionController;

        public JunctionBranchAspect(AspectBaseDefinition definition, BasicSignalController controller) : base(definition, controller)
        {
            _fullDef = (JunctionBranchAspectDefinition)definition;
            
            // Try to get junction controller directly, or from distant signal's home
            if (controller is JunctionSignalController junctionController)
            {
                _junctionController = junctionController;
            }
            else if (controller is DistantSignalController distantController && 
                     distantController.Home is JunctionSignalController homeJunctionController)
            {
                _junctionController = homeJunctionController;
            }
            else
            {
                _junctionController = null;
            }
        }

        public override bool MeetsConditions()
        {
            if (_junctionController == null) return false;

            return _fullDef.Mode switch
            {
                JunctionBranchAspectDefinition.JunctionAspectMode.ActiveOnThrough =>
                    _junctionController.Junction.IsSetToThrough(),
                JunctionBranchAspectDefinition.JunctionAspectMode.ActiveOnDiverging =>
                    !_junctionController.Junction.IsSetToThrough(),
                JunctionBranchAspectDefinition.JunctionAspectMode.ActiveOnBranch =>
                    _junctionController.Junction.selectedBranch == _fullDef.ActiveOnBranch,
                _ => false,
            };
        }
    }
}
