namespace Signals.API
{
    /// <summary>
    /// The direction a signal faces relative to its junction.
    /// </summary>
    public enum SignalDirection
    {
        /// <summary>
        /// The signal is not associated with a junction.
        /// </summary>
        None,
        /// <summary>
        /// The signal faces the diverging (outbound) branches of the junction.
        /// </summary>
        Out,
        /// <summary>
        /// The signal faces the converging (inbound) track of the junction.
        /// </summary>
        In
    }
}
