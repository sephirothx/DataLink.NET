using System;
using DataLink.NET.Interfaces;

namespace DataLink.NET
{
    public class DataLink
    {
        private readonly IPacketFactory        _packetFactory;
        private readonly ICommunicationChannel _communicationChannel;

        public event EventHandler<byte[]> PacketReceived;
        private void NotifyPacketReceived(byte[] packet) => PacketReceived?.Invoke(this, packet);

        public DataLink()
            : this(new PacketFactory(), new SerialPortProxy())
        {

        }

        public DataLink(IPacketFactory packetFactory,
                        ICommunicationChannel communicationChannel)
        {
            _packetFactory        = packetFactory;
            _communicationChannel = communicationChannel;

            _communicationChannel.DataReceived += OnDataReceived;
        }

        public string[] GetDeviceNames()
        {
            return _communicationChannel.GetDeviceNames();
        }

        public void SelectDevice(string device)
        {
            _communicationChannel.SelectDevice(device);
        }

        public void Send(byte[] payload)
        {
            var packet = _packetFactory.EncodePacket(payload);
            _communicationChannel.Send(packet);
        }

        public byte ReceiveByte()
        {
            return _communicationChannel.ReceiveByte();
        }

        private void OnDataReceived(object sender, EventArgs e)
        {
            while (_communicationChannel.BytesToRead > 0)
            {
                var nextReceived = _communicationChannel.ReceiveByte();
                var packet       = _packetFactory.ProcessNextByte(nextReceived);

                if (packet != null)
                {
                    NotifyPacketReceived(packet);
                }
            }
        }
    }
}
