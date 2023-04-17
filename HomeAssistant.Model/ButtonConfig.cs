using System.Text.Json.Serialization;

namespace HomeAssistant.Model;

public class ButtonConfig
{
    [JsonPropertyName("availability")]
    public Availability[] Availability { get; set; } = Array.Empty<Availability>();

    [JsonPropertyName("availability_mode")]
    public string AvailabilityMode { get; set; } = AvailabilityModes.All;

    [JsonPropertyName("command_topic")] 
    public string CommandTopic { get; set; } = "";

    [JsonPropertyName("device")] 
    public Device Device { get; set; } = Device.Empty;
    
    [JsonPropertyName("device_class")] 
    public string DeviceClass { get; set; } = ButtonDeviceClasses.DeviceClassNone;

    [JsonPropertyName("name")] 
    public string Name { get; set; } = "";

    [JsonPropertyName("object_id")] 
    public string ObjectID { get; set; } = "";

    [JsonPropertyName("payload_press")] 
    public string PayloadPress { get; set; } = Defaults.ButtonPayloadPress;

    [JsonPropertyName("unique_id")] 
    public string UniqueID { get; set; } = "";
}