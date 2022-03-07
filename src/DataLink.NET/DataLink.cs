using System;
using DataLink.NET.Interfaces;

namespace DataLink.NET
{
    /// <summary>
    /// Represents a customizable data link.
    /// </summary>
    public class DataLink
    {
        private readonly IPacketFormatter      _packetFormatter;
        private readonly ICommunicationChannel _communicationChannel;

        /// <summary>
        /// Occurs when a correctly formatted packet has been received.
        /// </summary>
        public event EventHandler<byte[]> PacketReceived;
        private void NotifyPacketReceived(byte[] packet) => PacketReceived?.Invoke(this, packet);

        /// <summary>
        /// Creates a new instance of <see cref="DataLink"/> with default
        /// <see cref="IPacketFormatter"/> and <see cref="ICommunicationChannel"/>.
        /// </summary>
        /// <remarks>
        /// <br>The default <see cref="IPacketFormatter"/> uses a DLE-STX + DLE-ETX stuffing.</br>
        /// <br>The default <see cref="ICommunicationChannel"/> uses a Serial Port with the following default values:</br>
        /// <code>
        /// BaudRate = 9600
        /// DataBits = 8
        /// Parity   = None
        /// StopBits = 1
        /// </code>
        /// </remarks>
        public DataLink()
            : this(new PacketFormatter(), new SerialPortProxy())
        {}

        /// <summary>
        /// Creates a new instance of <see cref="DataLink"/> with default
        /// <see cref="IPacketFormatter"/> and <see cref="ICommunicationChannel"/>,
        /// allowing to specify the options for the creation of the Serial Port.
        /// </summary>
        /// <param name="options"></param>
        public DataLink(SerialPortOptions options)
            : this(new PacketFormatter(), new SerialPortProxy(options))
        {}

        /// <summary>
        /// Creates a new instance of <see cref="DataLink"/> with the provided
        /// <see cref="IPacketFormatter"/> and the default <see cref="ICommunicationChannel"/>.
        /// </summary>
        /// <param name="packetFormatter"></param>
        public DataLink(IPacketFormatter packetFormatter)
            : this(packetFormatter, new SerialPortProxy())
        {}

        /// <summary>
        /// Creates a new instance of <see cref="DataLink"/> with the provided
        /// <see cref="ICommunicationChannel"/> and the default <see cref="IPacketFormatter"/>.
        /// </summary>
        /// <param name="communicationChannel"></param>
        public DataLink(ICommunicationChannel communicationChannel)
            : this(new PacketFormatter(), communicationChannel)
        {}

        /// <summary>
        /// Creates a new instance of <see cref="DataLink"/> with the provided
        /// <see cref="IPacketFormatter"/> and <see cref="ICommunicationChannel"/>.
        /// </summary>
        /// <param name="packetFormatter"></param>
        /// <param name="communicationChannel"></param>
        public DataLink(IPacketFormatter packetFormatter,
                        ICommunicationChannel communicationChannel)
        {
            _packetFormatter      = packetFormatter;
            _communicationChannel = communicationChannel;

            _communicationChannel.DataReceived += OnDataReceived;
        }

        /// <summary>
        /// Gets an array of available interface names.
        /// </summary>
        public string[] GetDeviceNames()
        {
            return _communicationChannel.GetDeviceNames();
        }

        /// <summary>
        /// Selects a specific interface as communication channel.
        /// </summary>
        /// <param name="device">The name of the selected interface.</param>
        public void SelectDevice(string device)
        {
            _communicationChannel.SelectDevice(device);
        }

        /// <summary>
        /// Formats and sends a packet over the communication channel.
        /// </summary>
        /// <param name="payload">The buffer containing the payload to send.</param>
        public void Send(byte[] payload)
        {
            var packet = _packetFormatter.EncodePacket(payload);
            _communicationChannel.Send(packet);
        }

        private void OnDataReceived(object sender, EventArgs e)
        {
            while (_communicationChannel.BytesToRead > 0)
            {
                var nextReceived = _communicationChannel.ReceiveByte();
                var packet       = _packetFormatter.ProcessNextByte(nextReceived);

                if (packet != null)
                {
                    NotifyPacketReceived(packet);
                }
            }
        }
    }
}
