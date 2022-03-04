using System;
using System.IO.Ports;
using DataLink.NET.Interfaces;

namespace DataLink.NET
{
    class SerialPortProxy : ICommunicationChannel
    {
        private SerialPort _serialPort;

        public event EventHandler DataReceived;

        public int BytesToRead => _serialPort.BytesToRead;

        public string[] GetDeviceNames()
        {
            return SerialPort.GetPortNames();
        }

        public void SelectDevice(string device)
        {
            _serialPort?.Dispose();

            _serialPort = new SerialPort(device, 9600);
            _serialPort.Open();

            _serialPort.DataReceived += OnDataReceived;
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
