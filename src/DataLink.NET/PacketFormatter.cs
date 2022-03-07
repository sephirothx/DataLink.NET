using System;
using System.Text;
using DataLink.NET.Helpers;
using DataLink.NET.Interfaces;

namespace DataLink.NET
{
    class PacketFormatter : IPacketFormatter
    {
        private const byte DLE = 0x10;
        private const byte STX = 0x02;
        private const byte ETX = 0x03;

        private readonly string _prefix = Encoding.ASCII.GetString(new[] { DLE, STX });
        private readonly string _suffix = Encoding.ASCII.GetString(new[] { DLE, ETX });
        private readonly string _dledle = Encoding.ASCII.GetString(new[] { DLE, DLE });
        private readonly string _dle    = Encoding.ASCII.GetString(new[] { DLE });

        private readonly PacketFormatterFsm _state = new();

        public byte[] EncodePacket(byte[] payload)
        {
            string packet = Encoding.ASCII.GetString(payload);
            packet = packet.Replace(_dle, _dledle);
            packet = $"{_prefix}{packet}{_suffix}";

            return Encoding.ASCII.GetBytes(packet);
        }

        public byte[] DecodePacket(byte[] packet)
        {
            string strPacket = Encoding.ASCII.GetString(packet);

            if (!strPacket.StartsWith(_prefix) ||
                !strPacket.EndsWith(_suffix))
            {
                throw new FormatException("Bad packet framing");
            }

            strPacket = strPacket.Remove(strPacket.Length - 2, 2)
                                 .Remove(0, 2)
                                 .Replace(_dledle, _dle);
            return Encoding.ASCII.GetBytes(strPacket);
        }

        public byte[] ProcessNextByte(byte nextByte)
        {
            switch (_state.CurrentState)
            {
            case PacketFormatterFsm.State.WaitingForDle when nextByte == DLE:
            case PacketFormatterFsm.State.WaitingForStx when nextByte == STX:
            case PacketFormatterFsm.State.WaitingForPayload when nextByte == DLE:
                _state.CurrentState = _state.NextState();
                break;

            case PacketFormatterFsm.State.WaitingForPayload:
                _state.Buffer.Add(nextByte);
                break;

            case PacketFormatterFsm.State.WaitingForDleOrEtx when nextByte == DLE:
                _state.Buffer.Add(nextByte);
                _state.CurrentState = _state.NextState();
                break;

            case PacketFormatterFsm.State.WaitingForDleOrEtx when nextByte == ETX:
                var packet = _state.Buffer.ToArray();
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
