using System.Text.Json.Serialization;

namespace HomeAssistant.Model;

public class BinarySensorConfig
{
    [JsonPropertyName("availability")]
    public Availability[] Availability { get; set; } = Array.Empty<Availability>();

    [JsonPropertyName("availability_mode")]
    public string AvailabilityMode { get; set; } = AvailabilityModes.All;

    [JsonPropertyName("device")] 
    public Device Device { get; set; } = Device.Empty;
    
    [JsonPropertyName("device_class")] 
    public string DeviceClass { get; set; } = "";

    [JsonPropertyName("name")] 
    public string Name { get; set; } = "";

    [JsonPropertyName("payload_off")] 
    public string PayloadOff { get; set; } = Defaults.PayloadOff;

    [JsonPropertyName("payload_on")] 
    public string PayloadOn { get; set; } = Defaults.PayloadOn;

    [JsonPropertyName("state_topic")] 
    public string StateTopic { get; set; } = "";

    [JsonPropertyName("unique_id")] 
    public string UniqueID { get; set; } = "";

    [JsonPropertyName("object_id")] 
    public string ObjectID { get; set; } = "";

    [JsonPropertyName("value_template")] 
    public string ValueTemplate { get; set; } = "";
}