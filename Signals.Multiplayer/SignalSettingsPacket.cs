using MPAPI.Interfaces.Packets;

namespace Signals.Multiplayer
{
    /// <summary>
    /// Packet sent from host to clients to enforce host-authoritative gameplay settings.
    /// Clients apply these values and block local saves while a session is active.
    /// </summary>
    public class SignalSettingsPacket : IPacket
    {
        public bool GenerateShuntingSignals { get; set; }
        public bool EnableSignalEnforcement { get; set; }
        public bool EnableMisalignedTrackOccupancy { get; set; }
    }
}
