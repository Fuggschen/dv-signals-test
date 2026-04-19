using MPAPI.Interfaces.Packets;

namespace Signals.Multiplayer
{
    /// <summary>
    /// Sent from host to clients to replicate a track reservation or clear.
    /// </summary>
    public class SignalReservationPacket : IPacket
    {
        /// <summary>
        /// The unique name of the signal that made (or cleared) the reservation.
        /// </summary>
        public string SignalId { get; set; } = string.Empty;

        /// <summary>
        /// Reservation duration in seconds.
        /// Greater than 0 means reserve for that many seconds; 0 or less means clear the reservation.
        /// </summary>
        public float Duration { get; set; }
    }
}
