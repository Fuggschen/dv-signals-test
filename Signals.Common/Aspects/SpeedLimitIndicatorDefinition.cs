namespace Signals.Common.Aspects
{
    public class SpeedLimitIndicatorDefinition : AspectBaseDefinition
    {
        /// <summary>
        /// The indicator activates when the track ahead has a speed limit
        /// lower than or equal to this value (in km/h).
        /// </summary>
        public int MaxSpeedLimit = 60;

        private void Reset()
        {
            Id = "SPEED_LIMIT_INDICATOR";
        }
    }
}
