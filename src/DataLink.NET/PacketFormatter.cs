using System;
using System.Collections.Generic;
using DataLink.NET.Helpers;
using DataLink.NET.Interfaces;

namespace DataLink.NET
{
    class PacketFormatter : IPacketFormatter
    {
        private const byte DLE = 0x10;
        private const byte STX = 0x02;
        private const byte ETX = 0x03;

        private readonly PacketFormatterFsm _state = new();
        private readonly object _syncRoot = new();

        public byte[] EncodePacket(byte[] payload)
        {
            ArgumentNullException.ThrowIfNull(payload);

            var packet = new List<byte>(payload.Length + 4);
            packet.Add(DLE);
            packet.Add(STX);

            foreach (byte current in payload)
            {
                packet.Add(current);
                if (current == DLE)
                {
                    packet.Add(DLE);
                }
            }

            packet.Add(DLE);
            packet.Add(ETX);

            return packet.ToArray();
        }

        public byte[] DecodePacket(byte[] packet)
        {
            ArgumentNullException.ThrowIfNull(packet);

            if (packet.Length < 4 ||
                packet[0] != DLE ||
                packet[1] != STX ||
                packet[^2] != DLE ||
                packet[^1] != ETX)
            {
                throw new FormatException("Bad packet framing");
            }

            var payload = new List<byte>(packet.Length - 4);

            for (int i = 2; i < packet.Length - 2; i++)
            {
                byte current = packet[i];
                if (current != DLE)
                {
                    payload.Add(current);
                    continue;
                }

                int nextIndex = i + 1;
                if (nextIndex >= packet.Length - 2)
                {
                    throw new FormatException("Bad packet stuffing");
                }

                byte escaped = packet[nextIndex];
                if (escaped != DLE)
                {
                    throw new FormatException("Bad packet stuffing");
                }

                payload.Add(DLE);
                i = nextIndex;
            }

            return payload.ToArray();
        }

        public void ProcessBytes(ReadOnlySpan<byte> bytes, Action<byte[]> onPacket)
        {
            ArgumentNullException.ThrowIfNull(onPacket);

            lock (_syncRoot)
            {
                foreach (byte nextByte in bytes)
                {
                    byte[] packet = ProcessNextByteCore(nextByte);
                    if (packet != null)
                    {
                        onPacket(packet);
                    }
                }
            }
        }

        private byte[] ProcessNextByteCore(byte nextByte)
        {
            switch (_state.CurrentState)
            {
            case PacketFormatterFsm.State.WaitingForDle when nextByte == DLE:
            case PacketFormatterFsm.State.WaitingForStx when nextByte == STX:
            case PacketFormatterFsm.State.WaitingForPayload when nextByte == DLE:
                _state.CurrentState = _state.NextState();
                break;

            case PacketFormatterFsm.State.WaitingForPayload:
                _state.AppendByte(nextByte);
                break;

            case PacketFormatterFsm.State.WaitingForDleOrEtx when nextByte == DLE:
                _state.AppendByte(nextByte);
                _state.CurrentState = _state.NextState();
                break;

            case PacketFormatterFsm.State.WaitingForDleOrEtx when nextByte == ETX:
                var packet = _state.Buffer.WrittenSpan.ToArray();
                _state.Reset();
                return packet;

            default:
                _state.Reset();
                break;
            }

            return null;
        }
    }
}
