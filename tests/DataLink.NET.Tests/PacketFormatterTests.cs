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
            var payload  = new byte[] { 0x00, 0x10, 0x20, 0x69 };
            var expected = new byte[]
            {
                0x10, 0x02,                   // DLE-STX - start
                0x00, 0x10, 0x10, 0x20, 0x69, // payload with escaped DLE
                0x10, 0x03                    // DLE-ETX - end
            };

            var packet = _formatter.EncodePacket(payload);
            Assert.IsTrue(expected.SequenceEqual(packet));
        }

        [TestMethod]
        public void DecodePacketTest()
        {
            var packet = new byte[]
            {
                0x10, 0x02,                   // DLE-STX - start
                0x00, 0x10, 0x10, 0x20, 0x69, // payload with escaped DLE
                0x10, 0x03                    // DLE-ETX - end
            };
            var expected = new byte[] { 0x00, 0x10, 0x20, 0x69 };

            var payload = _formatter.DecodePacket(packet);
            Assert.IsTrue(expected.SequenceEqual(payload));
        }

        [TestMethod]
        public void ProcessNextByteTest()
        {
            var packet = new byte[]
            {
                0x00, 0x03, 0x11,             // discard
                0x10, 0x02,                   // DLE-STX - start
                0x00, 0x10, 0x10, 0x20, 0x69, // payload with escaped DLE
                0x10, 0x03,                   // DLE-ETX - end
                0x10, 0x02                    // DLE-STX - start of a new packet
            };
            var expected = new byte[] { 0x00, 0x10, 0x20, 0x69 };

            byte[] payload = null;
            foreach (byte b in packet)
            {
                payload = _formatter.ProcessNextByte(b);
                if (payload != null) break;
            }

            Assert.IsTrue(expected.SequenceEqual(payload!));
        }
    }
}
