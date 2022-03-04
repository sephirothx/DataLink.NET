using System;
using System.Linq;
using DataLink.NET.Tests.Mock;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataLink.NET.Tests
{
    [TestClass]
    public class DataLinkTests
    {
        private DataLink              _serialTest;
        private MockCommunicationChannel _mockComm;

        [TestInitialize]
        public void MartaSerialTestInitialize()
        {
            var factory = new PacketFactory();
            _mockComm = new MockCommunicationChannel(factory);

            _serialTest = new DataLink(factory, _mockComm);
        }

        [TestMethod]
        public void GetDeviceNamesTest()
        {
            var names = _serialTest.GetDeviceNames();

            Assert.AreEqual("a", names[0]);
            Assert.AreEqual("b", names[1]);
            Assert.AreEqual("c", names[2]);
        }

        [TestMethod]
        public void SelectDeviceTest()
        {
            const string name = "z";
            _serialTest.SelectDevice(name);

            Assert.AreEqual(name, _mockComm.SelectedDevice);
        }

        [TestMethod]
        public void SendTest()
        {
            byte[] received = null;
            _serialTest.PacketReceived += (_, bytes) => received = bytes;

            var payload = new byte[] { 0x00, 0x10, 0x02, 0x10, 0x03 };
            _serialTest.Send(payload);

            Assert.IsTrue(payload.SequenceEqual(received));
        }

        [TestMethod]
        public void ReceiveByteTest()
        {
            Assert.ThrowsException<InvalidOperationException>(() => _serialTest.ReceiveByte());
        }
    }
}
