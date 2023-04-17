using System.Text.Json;
using Dvr;
using Microsoft.Extensions.Options;

namespace DvrMqtt.DvrMqtt;

public class MqttIntegration : IMqttIntegration
{
    private readonly IOptions<DvrMqttOptions> options_;
    private readonly JsonSerializerOptions jsonOptions_;

    public MqttIntegration(IOptions<DvrMqttOptions> options, JsonSerializerOptions jsonOptions)
    {
        options_ = options;
        jsonOptions_ = jsonOptions;
    }

    public string[] GetSwitches(DvrOptions options)
    {
        return options.IsNvr
            ? new[]
            {
                IMqttIntegration.SwitchVoiceEnable,
                IMqttIntegration.SwitchMailEnable,
            }
            : new[]
            {
                IMqttIntegration.SwitchVoiceEnable,
                IMqttIntegration.SwitchMailEnable,
                IMqttIntegration.SwitchLightEnable
            };
    }

    public string[] GetBinarySensors(DvrOptions options)
    {
        return options.IsNvr
            ? new[]
            {
                IMqttIntegration.BinarySensorIsProblemDetected
            }
            : new[]
            {
                IMqttIntegration.BinarySensorMotionDetectedAlarm,
                IMqttIntegration.BinarySensorIsProblemDetected
            };
    }

    public string[] GetButtons(DvrOptions _) => new[]
    {
        IMqttIntegration.CameraRestartButton
    };

    public string GetDescription(string entity)
    {
        return entity switch
        {
            IMqttIntegration.SwitchVoiceEnable => "Camera Voice Alarm",
            IMqttIntegration.SwitchMailEnable => "Camera Mail Alarm",
            IMqttIntegration.SwitchLightEnable => "Camera Light",
            IMqttIntegration.BinarySensorMotionDetectedAlarm => "Camera Motion",
            IMqttIntegration.BinarySensorIsProblemDetected => "Camera Problem",
            IMqttIntegration.CameraSnapshot => "Camera Snapshot",
            IMqttIntegration.CameraRestartButton => "Camera Restart",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public string ClientId => options_.Value.ClientId;

    public string GetStatusTopic() => 
        String.Join("/", options_.Value.TopicBase, "status");

    public string GetStatusTopic(string deviceName) => 
        String.Join("/", options_.Value.TopicBase, deviceName, "status");
    
    public string GetCommandTopic(string deviceName, string command) => 
        String.Join("/", options_.Value.TopicBase, deviceName, "set", command);

    public string GetStateTopic(string deviceName) => 
        String.Join("/", options_.Value.TopicBase, deviceName);

    public string GetStatePayload(DvrState state)
    {
        return JsonSerializer.Serialize(state, jsonOptions_);
    }

    public string GetCameraSnapshotTopic(string deviceName) =>
        String.Join("/", options_.Value.TopicBase, deviceName, IMqttIntegration.CameraSnapshot);

    public string GetButtonTopic(string deviceName, string button) =>
        String.Join("/", options_.Value.TopicBase, deviceName, button);
}