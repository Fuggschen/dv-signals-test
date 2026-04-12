namespace Signals.Common.Aspects
{
    public enum SignalModeCondition
    {
        Manual,
        Automatic
    }

    public class SignalModeAspectDefinition : AspectBaseDefinition
    {
        public SignalModeCondition ActiveOnMode = SignalModeCondition.Manual;

        private void Reset()
        {
            Id = "MODE_INDICATOR";
        }
    }
}
