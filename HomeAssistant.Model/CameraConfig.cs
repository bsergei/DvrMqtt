using System.Text.Json.Serialization;

namespace HomeAssistant.Model;

public class CameraConfig
{
    [JsonPropertyName("availability")] 
    public Availability[] Availability { get; set; } = Array.Empty<Availability>();

    [JsonPropertyName("availability_mode")]
    public string AvailabilityMode { get; set; } = AvailabilityModes.All;

    [JsonPropertyName("device")] 
    public Device Device { get; set; } = new();

    [JsonPropertyName("encoding")]
    public string Encoding { get; set; } = "";

    [JsonPropertyName("image_encoding")]
    public string ImageEncoding { get; set; } = "b64";

    [JsonPropertyName("enabled_by_default")]
    public bool EnabledByDefault { get; set; } = true;

    [JsonPropertyName("name")] 
    public string Name { get; set; } = "";

    [JsonPropertyName("unique_id")] 
    public string UniqueID { get; set; } = "";

    [JsonPropertyName("topic")] 
    public string Topic { get; set; } = "";
}