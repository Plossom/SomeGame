using System;
using Unity.Collections;
using Unity.Netcode;

namespace SomeGame.Online
{
    public enum SlotKind : byte { Empty, Human, Bot }

    /// <summary>One of the eight places in an online room: who sits there, their name and car colour, and whether they are ready.</summary>
    public struct LobbySlot : INetworkSerializable, IEquatable<LobbySlot>
    {
        public SlotKind Kind;
        public ulong ClientId;
        public FixedString32Bytes Name;
        public byte Color;
        public bool Ready;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Kind);
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref Color);
            serializer.SerializeValue(ref Ready);
        }

        public bool Equals(LobbySlot other) =>
            Kind == other.Kind && ClientId == other.ClientId && Name.Equals(other.Name) && Color == other.Color && Ready == other.Ready;
    }

    /// <summary>A car's state as sent over the network (30 times a second).</summary>
    public struct CarState : INetworkSerializable
    {
        public const byte Boost = 1, Elevated = 2, Hidden = 4, Finished = 8, Airborne = 16;

        public byte Slot;
        public int Race;          // which race of the room this belongs to (old packets are ignored)
        public double Stamp;      // when it was taken, on the sending phone's clock
        public UnityEngine.Vector2 Position;
        public float Rotation;
        public UnityEngine.Vector2 Velocity;
        public sbyte Drift;       // +1 left, -1 right, 0 none
        public byte Tier;         // drift sparks 0..3
        public byte Air;          // jump height 0..255
        public byte Flags;
        public ushort Checkpoints;
        public float FinishTime;  // seconds after GO

        public bool Has(byte flag) => (Flags & flag) != 0;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Slot);
            serializer.SerializeValue(ref Race);
            serializer.SerializeValue(ref Stamp);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Rotation);
            serializer.SerializeValue(ref Velocity);
            serializer.SerializeValue(ref Drift);
            serializer.SerializeValue(ref Tier);
            serializer.SerializeValue(ref Air);
            serializer.SerializeValue(ref Flags);
            serializer.SerializeValue(ref Checkpoints);
            serializer.SerializeValue(ref FinishTime);
        }
    }
}
