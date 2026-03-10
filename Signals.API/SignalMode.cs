namespace Signals.API
{
    /// <summary>
    /// The operating mode of a signal.
    /// </summary>
    public enum SignalMode
    {
        /// <summary>
        /// The signal aspect is determined by internal logic (track occupancy, junction state, etc.).
        /// </summary>
        Automatic,
        /// <summary>
        /// The signal aspect is locked to an externally-set value and will not be changed by internal logic.
        /// </summary>
        Manual
    }
}
