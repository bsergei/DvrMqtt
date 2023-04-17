using System.Text.Json;
using System.Text.Json.Serialization;
using Dvr.Commands.AdHoc;

namespace Dvr.Commands;

[DvrCommandId(1040)]
[JsonConverter(typeof(SetConfigRequestConverter))]
public class SetConfigRequest : IDvrRequest<SetConfigReply>
{
    public JsonElement Data { get; set; }

    public string? SessionID { get; set; }

    public string? Name { get; set; }
}