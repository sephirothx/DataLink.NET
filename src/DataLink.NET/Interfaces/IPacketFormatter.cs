namespace DataLink.NET.Interfaces
{
    public interface IPacketFormatter
    {
        byte[] EncodePacket(byte[] payload);
        byte[] DecodePacket(byte[] packet);
        byte[] ProcessNextByte(byte nextByte);
    }
}
