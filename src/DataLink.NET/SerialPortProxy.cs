using System;
using System.IO.Ports;
using DataLink.NET.Interfaces;

namespace DataLink.NET
{
    class SerialPortProxy : ICommunicationChannel
    {
        private readonly SerialPort _serialPort;

        public event EventHandler DataReceived;

        public int BytesToRead => _serialPort.BytesToRead;

        public SerialPortProxy()
            : this(new SerialPortOptions())
        {}

        public SerialPortProxy(SerialPortOptions options)
        {
            _serialPort = new SerialPort();

            _serialPort.BaudRate = options.BaudRate;
            _serialPort.Parity   = options.Parity;
            _serialPort.DataBits = options.DataBits;
            _serialPort.StopBits = options.StopBits;

            _serialPort.DataReceived += OnDataReceived;
        }

        public string[] GetDeviceNames()
        {
            return SerialPort.GetPortNames();
        }

        public void SelectDevice(string device)
        {
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
            _serialPort.Write(buffer, 0, buffer.Length);
        }

        public byte ReceiveByte()
        {
            return (byte)_serialPort.ReadByte();
        }
    }
}
