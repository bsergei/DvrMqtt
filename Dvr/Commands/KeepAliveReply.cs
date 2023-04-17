using System.Text.Json.Serialization;
using Dvr.Commands.AdHoc;

namespace Dvr.Commands;

[DvrCommandId(1007)]
public class KeepAliveReply : IDvrReply
{
    [JsonPropertyName("Ret")]
    public int Ret { get; set; }

    [JsonPropertyName("SessionID")]
    public string? SessionID { get; set; }

    [JsonPropertyName("Name")]
    public string? Name { get; set; }
}