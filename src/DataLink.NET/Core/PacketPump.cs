using System;
using System.Buffers;
using DataLink.NET.Interfaces;

namespace DataLink.NET.Core
{
    internal sealed class PacketPump : IDisposable
    {
        private readonly ICommunicationChannel _communicationChannel;
        private readonly IPacketFormatter _formatter;
        private readonly object _receiveLock = new();
        private bool _isStopped;

        public event Action<byte[]> PacketReceived;

        public PacketPump(ICommunicationChannel channel, IPacketFormatter formatter)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(formatter);

            _communicationChannel = channel;
            _formatter = formatter;
            _communicationChannel.DataReceived += OnDataReceived;
        }

        private void OnDataReceived(object sender, EventArgs e)
        {
            lock (_receiveLock)
            {
                if (_isStopped)
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
                        _formatter.ProcessBytes(readSpan, NotifyPacketReceived);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(chunk);
                    }
                }
            }
        }

        private void NotifyPacketReceived(byte[] packet)
        {
            PacketReceived?.Invoke(packet);
        }

        public void Dispose()
        {
            if (_isStopped)
            {
                return;
            }

            lock (_receiveLock)
            {
                if (_isStopped)
                {
                    return;
                }

                _isStopped = true;
                _communicationChannel.DataReceived -= OnDataReceived;
            }
        }
    }
}
