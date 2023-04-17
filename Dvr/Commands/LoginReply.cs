using System.Text.Json.Serialization;
using Dvr.Commands.AdHoc;

namespace Dvr.Commands;

[DvrCommandId(1001)]
public class LoginReply : IDvrReply
{
    [JsonPropertyName("Ret")]
    public int Ret { get; set; }

    [JsonPropertyName("SessionID")]
    public string? SessionID { get; set; }

    [JsonPropertyName("AliveInterval")]
    public int AliveInterval { get; set; }

    [JsonPropertyName("ChannelNum")]
    public int ChannelNum { get; set; }

    [JsonPropertyName("ExtraChannel")]
    public int ExtraChannel { get; set; }

    [JsonPropertyName("DeviceType ")]
    public string? DeviceType { get; set; }
}