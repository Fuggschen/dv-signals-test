using MPAPI.Interfaces.Packets;

namespace Signals.Multiplayer
{
    /// <summary>
    /// Packet for syncing a single signal's mode and aspect state.
    /// Sent from host to clients when a signal enters/exits Manual mode or changes aspect while in Manual mode.
    /// </summary>
    public class SignalStatePacket : IPacket
    {
        /// <summary>
        /// The unique name of the signal.
        /// </summary>
        public string SignalId { get; set; } = string.Empty;

        /// <summary>
        /// The aspect ID of the signal. Empty string means the signal is off.
        /// </summary>
        public string AspectId { get; set; } = string.Empty;

        /// <summary>
        /// The signal mode. 0 = Automatic, 1 = Manual.
        /// </summary>
        public byte Mode { get; set; }
    }
}
