using System;
using System.Buffers;
using DataLink.NET.Interfaces;

namespace DataLink.NET
{
    /// <summary>
    /// Represents a customizable data link.
    /// </summary>
    public class DataLink : IDisposable
    {
        private readonly IPacketFormatter      _packetFormatter;
        private readonly ICommunicationChannel _communicationChannel;
        private readonly object _receiveSync = new();
        private bool _disposed;

        /// <summary>
        /// Occurs when a correctly formatted packet has been received.
        /// </summary>
        public event EventHandler<byte[]> PacketReceived;

        /// <summary>
        /// Occurs when a packet event subscriber throws an exception.
        /// </summary>
        public event EventHandler<Exception> PacketHandlerError;

        private void NotifyPacketReceived(byte[] packet)
        {
            var handlers = PacketReceived;
            if (handlers == null)
            {
                return;
            }

            foreach (EventHandler<byte[]> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(this, packet);
                }
                catch (Exception exception)
                {
                    PacketHandlerError?.Invoke(this, exception);
                }
            }
        }

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
            : this(new PacketFormatter(), CreateSerialPortProxy(options))
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
            ArgumentNullException.ThrowIfNull(packetFormatter);
            ArgumentNullException.ThrowIfNull(communicationChannel);

            _packetFormatter      = packetFormatter;
            _communicationChannel = communicationChannel;

            _communicationChannel.DataReceived += OnDataReceived;
        }

        /// <summary>
        /// Gets an array of available interface names.
        /// </summary>
        public string[] GetDeviceNames()
        {
            ThrowIfDisposed();
            return _communicationChannel.GetDeviceNames();
        }

        /// <summary>
        /// Selects a specific interface as communication channel.
        /// </summary>
        /// <param name="device">The name of the selected interface.</param>
        public void SelectDevice(string device)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(device);
            _communicationChannel.SelectDevice(device);
        }

        /// <summary>
        /// Formats and sends a packet over the communication channel.
        /// </summary>
        /// <param name="payload">The buffer containing the payload to send.</param>
        public void Send(byte[] payload)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(payload);

            var packet = _packetFormatter.EncodePacket(payload);
            _communicationChannel.Send(packet);
        }

        private void OnDataReceived(object sender, EventArgs e)
        {
            lock (_receiveSync)
            {
                if (_disposed)
                {
                    return;
                }

                while (_communicationChannel.BytesToRead > 0)
                {
                    int bytesToRead = _communicationChannel.BytesToRead;
                    byte[] chunk = ArrayPool<byte>.Shared.Rent(bytesToRead);
                    try
                    {
                        int actualRead = _communicationChannel.Read(chunk, 0, bytesToRead);

                        if (actualRead == 0)
                        {
                            break;
                        }

                        var readSpan = new ReadOnlySpan<byte>(chunk, 0, actualRead);
                        _packetFormatter.ProcessBytes(readSpan, NotifyPacketReceived);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(chunk);
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _communicationChannel.DataReceived -= OnDataReceived;

            if (_communicationChannel is IDisposable disposable)
            {
                disposable.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        private static SerialPortProxy CreateSerialPortProxy(SerialPortOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return new SerialPortProxy(options);
        }
    }
}
