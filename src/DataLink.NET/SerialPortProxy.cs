using System;
using System.IO.Ports;
using DataLink.NET.Interfaces;

namespace DataLink.NET
{
    class SerialPortProxy : ICommunicationChannel, IDisposable
    {
        private readonly SerialPort _serialPort;
        private bool _disposed;

        public event EventHandler DataReceived;

        public int BytesToRead => _serialPort.BytesToRead;

        public SerialPortProxy()
            : this(new SerialPortOptions())
        {}

        public SerialPortProxy(SerialPortOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            _serialPort = new SerialPort();

            _serialPort.BaudRate = options.BaudRate;
            _serialPort.Parity   = options.Parity;
            _serialPort.DataBits = options.DataBits;
            _serialPort.StopBits = options.StopBits;

            _serialPort.DataReceived += OnDataReceived;
        }

        public string[] GetDeviceNames()
        {
            ThrowIfDisposed();
            return SerialPort.GetPortNames();
        }

        public void SelectDevice(string device)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(device);

            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }

            _serialPort.PortName = device;
            _serialPort.Open();
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            DataReceived?.Invoke(this, EventArgs.Empty);
        }

        public void Send(byte[] buffer)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(buffer);
            _serialPort.Write(buffer, 0, buffer.Length);
        }

        public byte ReceiveByte()
        {
            ThrowIfDisposed();
            return (byte)_serialPort.ReadByte();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _serialPort.DataReceived -= OnDataReceived;

            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }

            _serialPort.Dispose();
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }
}
