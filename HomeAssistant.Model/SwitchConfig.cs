using System.Text.Json.Serialization;

namespace HomeAssistant.Model;

public class SwitchConfig
{
    [JsonPropertyName("availability")] 
    public Availability[] Availability { get; set; } = Array.Empty<Availability>();

    [JsonPropertyName("availability_mode")]
    public string AvailabilityMode { get; set; } = AvailabilityModes.All;

    [JsonPropertyName("command_topic")]
    public string CommandTopic { get; set; } = "";

    [JsonPropertyName("device")] 
    public Device Device { get; set; } = new();

    [JsonPropertyName("entity_category")] 
    public string EntityCategory { get; set; } = "";

    [JsonPropertyName("icon")] 
    public string Icon { get; set; } = "";

    [JsonPropertyName("name")] 
    public string Name { get; set; } = "";

    [JsonPropertyName("payload_off")] 
    public string PayloadOff { get; set; } = Defaults.PayloadOff;

    [JsonPropertyName("payload_on")] 
    public string PayloadOn { get; set; } = Defaults.PayloadOn;

    [JsonPropertyName("state_off")] 
    public string StateOff { get; set; } = Defaults.PayloadOff;

    [JsonPropertyName("state_on")] 
    public string StateOn { get; set; } = Defaults.PayloadOn;

    [JsonPropertyName("state_topic")] 
    public string StateTopic { get; set; } = "";

    [JsonPropertyName("unique_id")] 
    public string UniqueID { get; set; } = "";

    [JsonPropertyName("value_template")] 
    public string ValueTemplate { get; set; } = "";

    [JsonPropertyName("optimistic")] 
    public bool Optimistic { get; set; } = false;
}