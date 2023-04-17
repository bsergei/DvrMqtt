using System.Text;
using System.Text.Json;
using Dvr.Commands.AdHoc;
using Microsoft.Extensions.Logging;

namespace Dvr;

public class DvrIpPacket : IDvrIpPacket
{
    private readonly ILogger<DvrIpPacket> logger_;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private const string ByteNotExpectedError = "Byte not expected";
    private const string UnexpectedCommandError = "Unexpected command";
    private const string ResponseIsTooShortError = "Response is too short";

    public DvrIpPacket(ILogger<DvrIpPacket> logger)
    {
        logger_ = logger;
    }

    public (T Data, uint SessionId) ParsePacket<T>(byte[] bytes, uint? seq = null)
    {
        var (doc, sessionId) = ParsePacket(
            bytes, 
            expectedCommandId:DvrCommandIdAttribute.GetCommandId<T>(), 
            expectedSeq: seq);

        return (doc.Deserialize<T>()!, sessionId);
    }

    public byte[] CreatePacket<T>(T data, uint session, uint seq)
    {
        var commandId = DvrCommandIdAttribute.GetCommandId<T>();
        if (commandId == null)
        {
            throw new InvalidOperationException($"Invalid command for {typeof(T)}");
        }

        var jsonData = JsonSerializer.Serialize(data, JsonOptions);
        var dataBytes = Encoding.ASCII.GetBytes(jsonData);

        return CreatePacket(session, seq, commandId.Value, dataBytes);
    }

    public bool TryGetSeq(byte[] bytes, out uint seq)
    {
        // Session is at position 8-11.
        if (bytes.Length < 11)
        {
            seq = default;
            return false;
        }

        var sessionBytes = bytes[8..12].AsSpan();
        seq = BitConverter.ToUInt32(sessionBytes);
        return true;
    }

    public bool TryGetSessionId(byte[] bytes, out uint sessionId)
    {
        // Session is at position 4-7.
        if (bytes.Length < 7)
        {
            sessionId = default;
            return false;
        }

        var sessionBytes = bytes[4..8].AsSpan();
        sessionId = BitConverter.ToUInt32(sessionBytes);
        return true;
    }

    public bool TryGetCommandId(byte[] bytes, out ushort commandId)
    {
        // Command is at position 14-15.
        if (bytes.Length < 15)
        {
            commandId = default;
            return false;
        }

        commandId = BitConverter.ToUInt16(bytes[14..16].AsSpan());
        return true;
    }

    public bool TryGetDataLength(byte[] bytes, out uint dataLength)
    {
        // Command is at position 16-19.
        if (bytes.Length < 19)
        {
            dataLength = default;
            return false;
        }

        dataLength = BitConverter.ToUInt16(bytes[16..20].AsSpan());
        return true;
    }

    public (byte[] Bytes, uint SessionId) ParsePacketBytes(
        byte[] bytes,
        ushort? expectedCommandId = null,
        uint? expectedSeq = null)
    {
        TryGetCommandId(bytes, out var cmdId);
        TryGetSeq(bytes, out var seq);

        if (bytes.Length <= 22)
        {
            throw new InvalidOperationException("Response is too short");
        }

        // Magic.
        Assert(bytes, 0, 0xff, 0x01); // Packet start.
        Assert(bytes, bytes.Length - 2, 0x0A, 0x00); // Packet end.

        // Session is at position 4-7.
        if (!TryGetSessionId(bytes, out var session))
        {
            throw new InvalidOperationException("Can't read session id from the response");
        }

        // PacketCount is as 8-11

        if (expectedSeq != null)
        {
            if (seq != expectedSeq.Value)
            {
                throw new InvalidOperationException(
                    $"Unexpected packet id: expected: {expectedSeq}, but received {seq}");
            }
        }

        if (expectedCommandId != null)
        {
            // Command is at position 14-15.
            if (cmdId != expectedCommandId.Value)
            {
                throw new InvalidOperationException(
                    $"{UnexpectedCommandError}: expected: {expectedCommandId}, but received {cmdId}");
            }
        }

        Assert(bytes, 19); // Data length is at 16-19.

        var lengthBytes = bytes[16..20].AsSpan();
        var length = (int)BitConverter.ToUInt32(lengthBytes);

        Assert(bytes, 20 + length - 1);

        var dataBytes = bytes[20..(20 + length)];

        logger_.LogDebug($"Parsed packet: session={session}, seq={seq}, cmd={cmdId}, dataLen={bytes?.Length}");

        return (dataBytes, session);
    }

    private (JsonDocument Data, uint SessionId) ParsePacket(
        byte[] bytes, 
        ushort? expectedCommandId = null, 
        uint? expectedSeq = null)
    {
        var p = ParsePacketBytes(bytes, expectedCommandId, expectedSeq);

        var json = Encoding.ASCII.GetString(p.Bytes).Trim('\r', '\n', (char)0);
        var doc = JsonDocument.Parse(json);

        return (doc, p.SessionId);
    }


    private byte[] CreatePacket(uint session, uint seq, ushort command, byte[]? data)
    {
        logger_.LogDebug($"Creating packet: session={session}, seq={seq}, cmd={command}, dataLen={data?.Length}");

        var packet = new List<byte>();

        // Magic.
        packet.Add(0xff);
        packet.Add(0x01);

        // Reserved.
        packet.Add(0x00);
        packet.Add(0x00);

        // Session 4-7.
        packet.AddRange(BitConverter.GetBytes(session));

        // Packet Count.
        packet.AddRange(BitConverter.GetBytes(seq));

        // Reserved.
        packet.Add(0x00);
        packet.Add(0x00);

        // Command.
        packet.AddRange(BitConverter.GetBytes(command));

        // Data Length.
        packet.AddRange(BitConverter.GetBytes(data?.Length ?? 0));

        // Data.
        packet.AddRange(data ?? Array.Empty<byte>());

        return packet.ToArray();
    }

    private static void Assert(IReadOnlyList<byte> bytes, int index, params byte[] bytesToCheck)
    {
        if (index + bytesToCheck.Length - 1 >= bytes.Count)
        {
            throw new InvalidOperationException(ResponseIsTooShortError);
        }

        for (int i = 0; i < bytesToCheck.Length; i++)
        {
            var a = bytes[index + i];
            var b = bytesToCheck[i];

            if (a != b)
            {
                throw new InvalidOperationException($"{ByteNotExpectedError} at {index + i}: Received {a} != Expected: {b}");
            }
        }
    }
}