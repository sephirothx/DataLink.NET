using System;
using System.Collections.Generic;

namespace DataLink.NET.Interfaces
{
    public interface IPacketFormatter
    {
        byte[] EncodePacket(byte[] payload);
        byte[] DecodePacket(byte[] packet);
        byte[] ProcessNextByte(byte nextByte);
        IReadOnlyList<byte[]> ProcessBytes(ReadOnlySpan<byte> bytes);
        void ProcessBytes(ReadOnlySpan<byte> bytes, Action<byte[]> onPacket);
    }
}
