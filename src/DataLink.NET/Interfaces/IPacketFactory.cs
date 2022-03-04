namespace DataLink.NET.Interfaces
{
    public interface IPacketFactory
    {
        byte[] EncodePacket(byte[] payload);
        byte[] DecodePacket(byte[] packet);
        byte[] ProcessNextByte(byte nextByte);
    }
}
