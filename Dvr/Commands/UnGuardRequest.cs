using System.Text.Json.Serialization;
using Dvr.Commands.AdHoc;

namespace Dvr.Commands;

[DvrCommandId(1502)]
public class UnGuardRequest : IDvrRequest<UnGuardReply>
{
    [JsonPropertyName("SessionID")]
    public string? SessionID { get; set; }
}