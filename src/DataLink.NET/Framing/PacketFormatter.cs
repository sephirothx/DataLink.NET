using System;
using System.Collections.Generic;
using DataLink.NET.Interfaces;

namespace DataLink.NET.Framing
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
                int index = 0;
                while (index < bytes.Length)
                {
                    if (_state.CurrentState == PacketFormatterFsm.State.WaitingForPayload)
                    {
                        ReadOnlySpan<byte> remaining = bytes[index..];
                        int dleIndex = remaining.IndexOf(DLE);

                        if (dleIndex < 0)
                        {
                            _state.AppendBytes(remaining);
                            break;
                        }

                        if (dleIndex > 0)
                        {
                            _state.AppendBytes(remaining[..dleIndex]);
                            index += dleIndex;
                        }
                    }

                    byte[] packet = ProcessNextByte(bytes[index]);
                    if (packet != null)
                    {
                        onPacket(packet);
                    }

                    index++;
                }
            }
        }

        private byte[] ProcessNextByte(byte nextByte)
        {
            PacketFormatterFsm.State currentState = _state.CurrentState;

            if (currentState == PacketFormatterFsm.State.WaitingForPayload)
            {
                if (nextByte == DLE)
                {
                    _state.CurrentState = PacketFormatterFsm.State.WaitingForDleOrEtx;
                    return null;
                }

                _state.AppendByte(nextByte);
                return null;
            }

            if (currentState == PacketFormatterFsm.State.WaitingForDleOrEtx)
            {
                if (nextByte == DLE)
                {
                    _state.AppendByte(nextByte);
                    _state.CurrentState = PacketFormatterFsm.State.WaitingForPayload;
                    return null;
                }

                if (nextByte == ETX)
                {
                    var packet = _state.Buffer.WrittenSpan.ToArray();
                    _state.Reset();
                    return packet;
                }

                if (nextByte == STX)
                {
                    // Recover from malformed in-packet framing by treating DLE-STX
                    // as the start of a new packet.
                    _state.Buffer.Clear();
                    _state.CurrentState = PacketFormatterFsm.State.WaitingForPayload;
                    return null;
                }

                _state.Reset();
                return null;
            }

            if (currentState == PacketFormatterFsm.State.WaitingForDle)
            {
                if (nextByte == DLE)
                {
                    _state.CurrentState = PacketFormatterFsm.State.WaitingForStx;
                }

                return null;
            }

            if (currentState == PacketFormatterFsm.State.WaitingForStx && nextByte == STX)
            {
                _state.CurrentState = PacketFormatterFsm.State.WaitingForPayload;
                return null;
            }

            _state.Reset();
            return null;
        }
    }
}
