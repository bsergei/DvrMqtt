using System.Text.Json.Serialization;
using Dvr.Commands.AdHoc;

namespace Dvr.Commands;

[DvrCommandId(1504)]
public class AlarmInfoDvrRequest
{
    [JsonPropertyName("AlarmInfo")]
    public AlarmInfo? AlarmInfo { get; set; }

    [JsonPropertyName("SessionID")]
    public string? SessionID { get; set; }

    [JsonPropertyName("Name")]
    public string? Name { get; set; }
}