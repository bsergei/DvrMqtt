using System.Text.Json.Serialization;
using Dvr.Commands.AdHoc;

namespace Dvr.Commands;

[DvrCommandId(1501)]
public class GuardReply : IDvrReply
{
    [JsonPropertyName("SessionID")]
    public string? SessionID { get; set; }

    [JsonPropertyName("Ret")]
    public int Ret { get; set; }
}