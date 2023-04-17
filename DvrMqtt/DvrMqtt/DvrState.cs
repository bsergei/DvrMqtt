using System.Text.Json.Serialization;
using HomeAssistant.Model;

namespace DvrMqtt.DvrMqtt;

public class DvrState
{
    [JsonPropertyName(IMqttIntegration.SwitchVoiceEnable)]
    public string VoiceEnable { get; set; } = Defaults.PayloadOff;

    [JsonPropertyName(IMqttIntegration.SwitchMailEnable)]
    public string MailEnable { get; set; } = Defaults.PayloadOff;

    [JsonPropertyName(IMqttIntegration.BinarySensorMotionDetectedAlarm)]
    public string MotionDetectedAlarm { get; set; } = Defaults.PayloadOff;

    [JsonPropertyName(IMqttIntegration.SwitchLightEnable)]
    public string CameraLight { get; set; } = Defaults.PayloadOff;

    [JsonPropertyName(IMqttIntegration.BinarySensorIsProblemDetected)]
    public string IsProblemDetected { get; set; } = Defaults.PayloadOff;
}