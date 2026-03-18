using System;
using System.Linq;
using System.Collections.Generic;
using DataLink.NET.Framing;
using DataLink.NET.Tests.Mock;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataLink.NET.Tests
{
    [TestClass]
    public class DataLinkTests
    {
        private DataLink _dataLink;
        private MockCommunicationChannel _mockChannel;

        [TestInitialize]
        public void DataLinkTestInitialize()
        {
            var packetFormatter = new PacketFormatter();
            _mockChannel = new MockCommunicationChannel(packetFormatter);

            _dataLink = new DataLink(packetFormatter, _mockChannel);
        }

        [TestCleanup]
        public void DataLinkTestCleanup()
        {
            _dataLink?.Dispose();
        }

        [TestMethod]
        public void GetDeviceNamesTest()
        {
            var names = _dataLink.GetDeviceNames();

            Assert.AreEqual("a", names[0]);
            Assert.AreEqual("b", names[1]);
            Assert.AreEqual("c", names[2]);
        }

        [TestMethod]
        public void SelectDeviceTest()
        {
            const string name = "z";
            _dataLink.SelectDevice(name);

            Assert.AreEqual(name, _mockChannel.SelectedDevice);
        }

        [TestMethod]
        public void SendTest()
        {
            byte[] received = null;
            _dataLink.PacketReceived += (_, bytes) => received = bytes;

            var payload = new byte[] { 0x00, 0x10, 0x02, 0xFF, 0xD0, 0x10, 0x03 };
            _dataLink.Send(payload);

            Assert.IsTrue(payload.SequenceEqual(received));
        }

        [TestMethod]
        public void ConstructorWithNullFormatterThrows()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new DataLink(null, _mockChannel));
        }

        [TestMethod]
        public void ConstructorWithNullChannelThrows()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new DataLink(new PacketFormatter(), null));
        }

        [TestMethod]
        public void SendWithNullPayloadThrows()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => _dataLink.Send(null));
        }

        [TestMethod]
        public void SelectDeviceWithNullNameThrows()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => _dataLink.SelectDevice(null));
        }

        [TestMethod]
        public void DisposeUnsubscribesAndDisposesChannel()
        {
            Assert.AreEqual(1, _mockChannel.DataReceivedSubscribers);

            _dataLink.Dispose();
            _dataLink.Dispose();

            Assert.AreEqual(0, _mockChannel.DataReceivedSubscribers);
            Assert.IsTrue(_mockChannel.IsDisposed);
        }

        [TestMethod]
        public void MethodsThrowAfterDispose()
        {
            _dataLink.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() => _dataLink.GetDeviceNames());
            Assert.ThrowsExactly<ObjectDisposedException>(() => _dataLink.SelectDevice("a"));
            Assert.ThrowsExactly<ObjectDisposedException>(() => _dataLink.Send(new byte[] { 0x01 }));
        }

        [TestMethod]
        public void PacketReceivedHandlerExceptionIsReportedAndDoesNotStopOtherHandlers()
        {
            var receivedPackets = new List<byte[]>();
            Exception reported = null;

            _dataLink.PacketReceived += (_, _) => throw new InvalidOperationException("handler failure");
            _dataLink.PacketReceived += (_, payload) => receivedPackets.Add(payload);
            _dataLink.PacketHandlerError += (_, exception) => reported = exception;

            var payloadToSend = new byte[] { 0x41, 0x42, 0x43 };
            _dataLink.Send(payloadToSend);

            Assert.AreEqual(1, receivedPackets.Count);
            Assert.IsTrue(payloadToSend.SequenceEqual(receivedPackets[0]));
            Assert.IsNotNull(reported);
            Assert.IsInstanceOfType<InvalidOperationException>(reported);
        }
    }
}
