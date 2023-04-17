using System.Text.Json.Serialization;
using Dvr.Commands.AdHoc;

namespace Dvr.Commands;

[DvrCommandId(1042)]
public class GetConfigRequest : IDvrRequest<GetConfigReply>
{
    [JsonPropertyName("SessionID")]
    public string? SessionID { get; set; }

    [JsonPropertyName("Name")]
    public string? Name { get; set; }
}