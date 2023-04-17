using System.Text.Json.Serialization;

namespace HomeAssistant.Model;

public class Device
{
    public static readonly Device Empty = new();

    [JsonPropertyName("identifiers")]
    public string[] Identifiers { get; set; } = Array.Empty<string>();

    [JsonPropertyName("manufacturer")] 
    public string Manufacturer { get; set; } = "";

    [JsonPropertyName("model")] 
    public string Model { get; set; } = "";

    [JsonPropertyName("name")] 
    public string Name { get; set; } = "";
}