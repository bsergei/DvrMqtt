using System.Text.Json.Serialization;
using Dvr.Commands.AdHoc;

namespace Dvr.Commands;

[DvrCommandId(1450)]
public class SystemRequest : IDvrRequest<SystemReply>
{
    public SystemRequest()
    {
        Name = "OPMachine";
    }

    [JsonPropertyName("OPMachine")]
    public OpMachine? OpMachine { get; set; }

    [JsonPropertyName("SessionID")]
    public string? SessionID { get; set; }

    [JsonPropertyName("Name")]
    public string? Name { get; set; }
}