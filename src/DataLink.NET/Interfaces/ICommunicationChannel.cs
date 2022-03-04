using System;

namespace DataLink.NET.Interfaces
{
    public interface ICommunicationChannel
    {
        event EventHandler DataReceived;

        int BytesToRead { get; }

        string[] GetDeviceNames();
        void SelectDevice(string device);
        void Send(byte[] buffer);
        byte ReceiveByte();
    }
}
