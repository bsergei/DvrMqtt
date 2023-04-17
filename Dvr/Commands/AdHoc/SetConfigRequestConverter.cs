using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dvr.Commands.AdHoc;

public class SetConfigRequestConverter : JsonConverter<SetConfigRequest>
{
    public override SetConfigRequest Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, SetConfigRequest value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString(nameof(SetConfigRequest.SessionID), value.SessionID);
        writer.WriteString(nameof(SetConfigRequest.Name), value.Name);

        if (value.Name != null)
        {
            writer.WritePropertyName(value.Name);
            writer.WriteRawValue(value.Data.ToString(), true);
        }

        writer.WriteEndObject();
    }
}