using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataLink.NET.Tests
{
    [TestClass]
    public class PacketFormatterTests
    {
        private PacketFormatter _formatter;

        [TestInitialize]
        public void PacketFormatterTestInitialize()
        {
            _formatter = new PacketFormatter();
        }

        [TestMethod]
        public void EncodePacketTest()
        {
            var payload = new byte[] { 0x00, 0x10, 0x69, 0xFF, 0xD0 };
            var expected = new byte[]
            {
                0x10, 0x02,                         // DLE-STX - start
                0x00, 0x10, 0x10, 0x69, 0xFF, 0xD0, // payload with escaped DLE
                0x10, 0x03                          // DLE-ETX - end
            };

            var packet = _formatter.EncodePacket(payload);
            Assert.IsTrue(expected.SequenceEqual(packet));
        }

        [TestMethod]
        public void DecodePacketTest()
        {
            var packet = new byte[]
            {
                0x10, 0x02,                         // DLE-STX - start
                0x00, 0x10, 0x10, 0x69, 0xFF, 0xD0, // payload with escaped DLE
                0x10, 0x03                          // DLE-ETX - end
            };
            var expected = new byte[] { 0x00, 0x10, 0x69, 0xFF, 0xD0 };

            var payload = _formatter.DecodePacket(packet);
            Assert.IsTrue(expected.SequenceEqual(payload));
        }

        [TestMethod]
        public void ProcessNextByteTest()
        {
            var packet = new byte[]
            {
                0x10, 0x03, 0x11,                   // discard
                0x10, 0x02,                         // DLE-STX - start
                0x00, 0x10, 0x10, 0x69, 0xFF, 0xD0, // payload with escaped DLE
                0x10, 0x03,                         // DLE-ETX - end
                0x10, 0x02                          // DLE-STX - start of a new packet
            };
            var expected = new byte[] { 0x00, 0x10, 0x69, 0xFF, 0xD0 };

            byte[] payload = null;
            foreach (byte b in packet)
            {
                payload = _formatter.ProcessNextByte(b);
                if (payload != null) break;
            }

            Assert.IsTrue(expected.SequenceEqual(payload!));
        }

        [TestMethod]
        public void EncodePacketWithNullPayloadThrows()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => _formatter.EncodePacket(null));
        }

        [TestMethod]
        public void DecodePacketWithNullPacketThrows()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => _formatter.DecodePacket(null));
        }

        [TestMethod]
        public void DecodePacketWithInvalidStuffingThrows()
        {
            var malformed = new byte[]
            {
                0x10, 0x02,
                0x99, 0x10, 0x01,
                0x10, 0x03
            };

            Assert.ThrowsExactly<FormatException>(() => _formatter.DecodePacket(malformed));
        }

        [TestMethod]
        public void DecodePacketWithDanglingDleThrows()
        {
            var malformed = new byte[]
            {
                0x10, 0x02,
                0x99, 0x10,
                0x10, 0x03
            };

            Assert.ThrowsExactly<FormatException>(() => _formatter.DecodePacket(malformed));
        }

        [TestMethod]
        public void ProcessBytesReturnsAllCompletedPackets()
        {
            var stream = new byte[]
            {
                0x55, 0x10, 0x03, // discarded noise
                0x10, 0x02,
                0x41, 0x42,
                0x10, 0x03,
                0x10, 0x02,
                0x10, 0x10, 0x99,
                0x10, 0x03
            };

            var packets = _formatter.ProcessBytes(stream);

            Assert.AreEqual(2, packets.Count);
            CollectionAssert.AreEqual(new byte[] { 0x41, 0x42 }, packets[0]);
            CollectionAssert.AreEqual(new byte[] { 0x10, 0x99 }, packets[1]);
        }

        [TestMethod]
        public void ProcessBytesKeepsStateAcrossChunks()
        {
            var firstChunk = new byte[] { 0x10, 0x02, 0xAA };
            var secondChunk = new byte[] { 0x10, 0x10, 0xBB, 0x10, 0x03 };

            var firstPackets = _formatter.ProcessBytes(firstChunk);
            var secondPackets = _formatter.ProcessBytes(secondChunk);

            Assert.AreEqual(0, firstPackets.Count);
            Assert.AreEqual(1, secondPackets.Count);
            CollectionAssert.AreEqual(new byte[] { 0xAA, 0x10, 0xBB }, secondPackets[0]);
        }

        [TestMethod]
        public void ProcessBytesCallbackReturnsAllCompletedPackets()
        {
            var stream = new byte[]
            {
                0x55, 0x10, 0x03,
                0x10, 0x02,
                0x41, 0x42,
                0x10, 0x03,
                0x10, 0x02,
                0x10, 0x10, 0x99,
                0x10, 0x03
            };
            var packets = new List<byte[]>();

            _formatter.ProcessBytes(stream, packet => packets.Add(packet));

            Assert.AreEqual(2, packets.Count);
            CollectionAssert.AreEqual(new byte[] { 0x41, 0x42 }, packets[0]);
            CollectionAssert.AreEqual(new byte[] { 0x10, 0x99 }, packets[1]);
        }

        [TestMethod]
        public void ProcessBytesWithNullCallbackThrows()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => _formatter.ProcessBytes(new byte[] { 0x10 }, null));
        }
    }
}
