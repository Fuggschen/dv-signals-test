using MPAPI.Interfaces.Packets;
using System.Collections.Generic;
using System.IO;

namespace Signals.Multiplayer
{
    /// <summary>
    /// Packet containing the state of all manually-controlled signals.
    /// Sent from host to a joining client so they receive the current manual overrides.
    /// </summary>
    public class SignalFullSyncPacket : ISerializablePacket
    {
        public List<SignalEntry> Signals { get; set; } = new List<SignalEntry>();

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Signals.Count);

            foreach (var entry in Signals)
            {
                writer.Write(entry.SignalId);
                writer.Write(entry.AspectId);
                writer.Write(entry.Mode);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            Signals = new List<SignalEntry>(count);

            for (int i = 0; i < count; i++)
            {
                Signals.Add(new SignalEntry
                {
                    SignalId = reader.ReadString(),
                    AspectId = reader.ReadString(),
                    Mode = reader.ReadByte()
                });
            }
        }

        public struct SignalEntry
        {
            public string SignalId;
            public string AspectId;
            public byte Mode;
        }
    }
}
