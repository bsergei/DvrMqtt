using System.Text.Json.Serialization;
using Dvr.Commands.AdHoc;

namespace Dvr.Commands;

[DvrCommandId(1006)]
public class KeepAliveRequest : IDvrRequest<KeepAliveReply>
{
    public KeepAliveRequest()
    {
        Name = "KeepAlive";
    }

    [JsonPropertyName("SessionID")]
    public string? SessionID { get; set; }

    [JsonPropertyName("Name")]
    public string? Name { get; set; }
}