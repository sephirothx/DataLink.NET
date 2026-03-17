using System;
using System.Linq;
using System.Collections.Generic;
using DataLink.NET.Tests.Mock;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataLink.NET.Tests
{
    [TestClass]
    public class DataLinkTests
    {
        private DataLink                 _serialTest;
        private MockCommunicationChannel _mockComm;

        [TestInitialize]
        public void MartaSerialTestInitialize()
        {
            var factory = new PacketFormatter();
            _mockComm = new MockCommunicationChannel(factory);

            _serialTest = new DataLink(factory, _mockComm);
        }

        [TestCleanup]
        public void DataLinkTestCleanup()
        {
            _serialTest?.Dispose();
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

            var payload = new byte[] { 0x00, 0x10, 0x02, 0xFF, 0xD0, 0x10, 0x03 };
            _serialTest.Send(payload);

            Assert.IsTrue(payload.SequenceEqual(received));
        }

        [TestMethod]
        public void ConstructorWithNullFormatterThrows()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new DataLink(null, _mockComm));
        }

        [TestMethod]
        public void ConstructorWithNullChannelThrows()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new DataLink(new PacketFormatter(), null));
        }

        [TestMethod]
        public void SendWithNullPayloadThrows()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => _serialTest.Send(null));
        }

        [TestMethod]
        public void SelectDeviceWithNullNameThrows()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => _serialTest.SelectDevice(null));
        }

        [TestMethod]
        public void DisposeUnsubscribesAndDisposesChannel()
        {
            Assert.AreEqual(1, _mockComm.DataReceivedSubscribers);

            _serialTest.Dispose();
            _serialTest.Dispose();

            Assert.AreEqual(0, _mockComm.DataReceivedSubscribers);
            Assert.IsTrue(_mockComm.IsDisposed);
        }

        [TestMethod]
        public void MethodsThrowAfterDispose()
        {
            _serialTest.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() => _serialTest.GetDeviceNames());
            Assert.ThrowsExactly<ObjectDisposedException>(() => _serialTest.SelectDevice("a"));
            Assert.ThrowsExactly<ObjectDisposedException>(() => _serialTest.Send(new byte[] { 0x01 }));
        }

        [TestMethod]
        public void PacketReceivedHandlerExceptionIsReportedAndDoesNotStopOtherHandlers()
        {
            var receivedPackets = new List<byte[]>();
            Exception reported = null;

            _serialTest.PacketReceived += (_, _) => throw new InvalidOperationException("handler failure");
            _serialTest.PacketReceived += (_, payload) => receivedPackets.Add(payload);
            _serialTest.PacketHandlerError += (_, exception) => reported = exception;

            var payloadToSend = new byte[] { 0x41, 0x42, 0x43 };
            _serialTest.Send(payloadToSend);

            Assert.AreEqual(1, receivedPackets.Count);
            Assert.IsTrue(payloadToSend.SequenceEqual(receivedPackets[0]));
            Assert.IsNotNull(reported);
            Assert.IsInstanceOfType<InvalidOperationException>(reported);
        }
    }
}
