using System;
using System.Buffers;

namespace DataLink.NET.Helpers
{
    class PacketFormatterFsm
    {
        public enum State
        {
            WaitingForDle,
            WaitingForStx,
            WaitingForPayload,
            WaitingForDleOrEtx
        }

        public ArrayBufferWriter<byte> Buffer { get; } = new();

        public State CurrentState { get; set; } = State.WaitingForDle;

        public void Reset()
        {
            Buffer.Clear();
            CurrentState = State.WaitingForDle;
        }

        public void AppendByte(byte value)
        {
            Span<byte> destination = Buffer.GetSpan(1);
            destination[0] = value;
            Buffer.Advance(1);
        }

        public State NextState() => CurrentState switch
        {
            State.WaitingForDleOrEtx => State.WaitingForPayload,
            _                        => CurrentState + 1
        };
    }
}
