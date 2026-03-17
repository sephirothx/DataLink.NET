using System;

namespace DataLink.NET.Interfaces
{
    public interface IPacketFormatter
    {
        byte[] EncodePacket(byte[] payload);
        byte[] DecodePacket(byte[] packet);
        void ProcessBytes(ReadOnlySpan<byte> bytes, Action<byte[]> onPacket);
    }
}
