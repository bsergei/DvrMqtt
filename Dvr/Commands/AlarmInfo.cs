using System.Text.Json.Serialization;

namespace Dvr.Commands;

public class AlarmInfo
{
    public const string MotionDetectEvent = "appEventHumanDetectAlarm";

    public const string StatusStart = "Start";
    public const string StatusEnd = "End";

    [JsonPropertyName("Channel")]
    public int Channel { get; set; }

    [JsonPropertyName("Event")]
    public string? Event { get; set; }
    
    [JsonPropertyName("StartTime")]
    public string? StartTime { get; set; }
    
    [JsonPropertyName("Status")]
    public string? Status { get; set; }
}