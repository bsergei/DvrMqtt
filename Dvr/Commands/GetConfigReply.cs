using System.Text.Json;
using System.Text.Json.Serialization;
using Dvr.Commands.AdHoc;

namespace Dvr.Commands;

[JsonConverter(typeof(GetConfigReplyConverter))]
[DvrCommandId(1043)]
public class GetConfigReply : IDvrReply
{
    public JsonElement Data { get; set; }

    public int Ret { get; set; }

    public string? SessionID { get; set; }

    public string? Name { get; set; }
}