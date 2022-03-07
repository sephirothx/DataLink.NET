using System.Collections.Generic;

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

        public List<byte> Buffer { get; set; } = new();

        public State CurrentState { get; set; } = State.WaitingForDle;

        public void Reset()
        {
            Buffer       = new List<byte>();
            CurrentState = State.WaitingForDle;
        }

        public State NextState() => CurrentState switch
        {
            State.WaitingForDleOrEtx => State.WaitingForPayload,
            _                        => CurrentState + 1
        };
    }
}
