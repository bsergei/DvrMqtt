namespace Dvr;

public interface IDvrIpPacket
{
    (T Data, uint SessionId) ParsePacket<T>(byte[] bytes, uint? seq = null);

    byte[] CreatePacket<T>(T data, uint session, uint seq);

    bool TryGetSeq(byte[] bytes, out uint seq);

    bool TryGetSessionId(byte[] bytes, out uint sessionId);

    bool TryGetCommandId(byte[] bytes, out ushort commandId);

    bool TryGetDataLength(byte[] bytes, out uint dataLength);

    (byte[] Bytes, uint SessionId) ParsePacketBytes(
        byte[] bytes,
        ushort? expectedCommandId = null,
        uint? expectedSeq = null);
}