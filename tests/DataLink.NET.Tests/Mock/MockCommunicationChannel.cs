using System;
using DataLink.NET.Interfaces;

namespace DataLink.NET.Tests.Mock
{
    class MockCommunicationChannel : ICommunicationChannel, IDisposable
    {
        private readonly PacketFormatter _packetFormatter;

        private byte[] _toSend;
        private EventHandler _dataReceived;

        public string SelectedDevice { get; private set; }

        public bool IsDisposed { get; private set; }

        public int DataReceivedSubscribers { get; private set; }

        public event EventHandler DataReceived
        {
            add
            {
                _dataReceived += value;
                DataReceivedSubscribers++;
            }
            remove
            {
                _dataReceived -= value;
                DataReceivedSubscribers--;
            }
        }

        public int BytesToRead { get; private set; }

        public MockCommunicationChannel(PacketFormatter packetFormatter)
        {
            ArgumentNullException.ThrowIfNull(packetFormatter);
            _packetFormatter = packetFormatter;
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
            var received = _packetFormatter.DecodePacket(buffer);
            _toSend = _packetFormatter.EncodePacket(received);

            BytesToRead = _toSend.Length;
            _dataReceived?.Invoke(this, EventArgs.Empty);
        }

        public byte ReceiveByte()
        {
            if (BytesToRead > 0)
            {
                return _toSend[^BytesToRead--];
            }

            throw new InvalidOperationException();
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
