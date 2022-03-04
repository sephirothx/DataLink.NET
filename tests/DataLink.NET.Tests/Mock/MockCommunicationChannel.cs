using System;
using DataLink.NET.Interfaces;

namespace DataLink.NET.Tests.Mock
{
    class MockCommunicationChannel : ICommunicationChannel
    {
        private readonly PacketFactory _packetFactory;

        private byte[] _toSend;

        public string SelectedDevice { get; private set; }

        public event EventHandler DataReceived;

        public int BytesToRead { get; private set; }

        public MockCommunicationChannel(PacketFactory packetFactory)
        {
            _packetFactory = packetFactory;
        }

        public string[] GetDeviceNames()
        {
            return new[] { "a", "b", "c" };
        }

        public void SelectDevice(string device)
        {
            SelectedDevice = device;
        }

        public void Send(byte[] buffer)
        {
            var received = _packetFactory.DecodePacket(buffer);
            _toSend = _packetFactory.EncodePacket(received);

            BytesToRead = _toSend.Length;
            DataReceived?.Invoke(this, EventArgs.Empty);
        }

        public byte ReceiveByte()
        {
            if (BytesToRead > 0)
            {
                return _toSend[^BytesToRead--];
            }

            throw new InvalidOperationException();
        }
    }
}
