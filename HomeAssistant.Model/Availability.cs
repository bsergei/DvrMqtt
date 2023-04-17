using System.Text.Json.Serialization;

namespace HomeAssistant.Model;

public class Availability
{
    [JsonPropertyName("topic")]
    public string Topic { get; set; } = "";

    [JsonPropertyName("payload_available")]
    public string PayloadAvailable { get; set; } = Defaults.StatusOnline;

    [JsonPropertyName("payload_not_available")]
    public string PayloadNotAvailable { get; set; } = Defaults.StatusOffline;
}