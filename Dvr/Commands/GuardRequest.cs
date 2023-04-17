using System.Text.Json.Serialization;
using Dvr.Commands.AdHoc;

namespace Dvr.Commands;

[DvrCommandId(1500)]
public class GuardRequest : IDvrRequest<GuardReply>
{
    [JsonPropertyName("SessionID")]
    public string? SessionID { get; set; }
}