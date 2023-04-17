using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dvr.Commands.AdHoc;

public class GetConfigReplyConverter : JsonConverter<GetConfigReply>
{
    public override GetConfigReply? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var doc = JsonDocument.ParseValue(ref reader);
        var rootElement = doc.RootElement;
        var name = rootElement.GetProperty(nameof(GetConfigReply.Name)).GetString();

        return new GetConfigReply
        {
            Ret = rootElement.GetProperty(nameof(GetConfigReply.Ret)).GetInt32(),
            Name = name,
            SessionID = rootElement.GetProperty(nameof(GetConfigReply.SessionID)).GetString(),
            Data = rootElement.TryGetProperty(name!, out var dataElement) ? dataElement : default,
        };
    }

    public override void Write(Utf8JsonWriter writer, GetConfigReply value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}