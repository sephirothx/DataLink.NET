using System;
using System.Text;
using DataLink.NET.Helpers;
using DataLink.NET.Interfaces;

namespace DataLink.NET
{
    class PacketFactory : IPacketFactory
    {
        private const byte DLE = 0x10;
        private const byte STX = 0x02;
        private const byte ETX = 0x03;

        private readonly string _prefix = Encoding.ASCII.GetString(new[] { DLE, STX });
        private readonly string _suffix = Encoding.ASCII.GetString(new[] { DLE, ETX });
        private readonly string _dledle = Encoding.ASCII.GetString(new[] { DLE, DLE });
        private readonly string _dle    = Encoding.ASCII.GetString(new[] { DLE });

        private readonly PacketFactoryFsm _state = new();

        private DateTime _start;

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
            Console.Write((char)nextByte);
            switch (_state.CurrentState)
            {
            case PacketFactoryFsm.State.WaitingForDle:
                if (nextByte == DLE)
                {
                    _start              = DateTime.Now;
                    _state.CurrentState = PacketFactoryFsm.State.WaitingForStx;
                }
                break;

            case PacketFactoryFsm.State.WaitingForStx:
                if (nextByte == STX)
                {
                    _state.CurrentState = PacketFactoryFsm.State.WaitingForPayload;
                }
                else
                {
                    _state.Reset();
                }
                break;

            case PacketFactoryFsm.State.WaitingForPayload:
                if (nextByte == DLE)
                {
                    _state.CurrentState = PacketFactoryFsm.State.WaitingForDleOrEtx;
                }
                else
                {
                    _state.Buffer.Add(nextByte);
                }
                break;

            case PacketFactoryFsm.State.WaitingForDleOrEtx:
                if (nextByte == DLE)
                {
                    _state.Buffer.Add(nextByte);
                    _state.CurrentState = PacketFactoryFsm.State.WaitingForPayload;
                }
                else if (nextByte == ETX)
                {
                    _state.CurrentState = PacketFactoryFsm.State.WaitingForDle;
                    var packet = _state.Buffer.ToArray();
                    _state.Reset();

                    Console.WriteLine(DateTime.Now - _start);

                    return packet;
                }
                else
                {
                    _state.Reset();
                }
                break;

            default:
                _state.Reset();
                break;
            }

            return null;
        }
    }
}
