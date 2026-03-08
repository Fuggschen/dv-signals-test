using UnityEngine;

namespace Signals.Common.Aspects
{
    /// <summary>
    /// Aspect that is active when the junction is aligned for the shunting signal's branch.
    /// Used for shunting signals to show PROCEED (white) only when junction is correctly set.
    /// </summary>
    public class JunctionAlignmentAspectDefinition : AspectBaseDefinition
    {
        [Space]
        [Tooltip("If true, aspect is active when junction is ALIGNED. If false, active when MISALIGNED.")]
        public bool ActiveWhenAligned = true;
    }
}

