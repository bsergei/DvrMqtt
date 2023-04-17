using Dvr;

namespace DvrMqtt.DvrMqtt;

public interface IMqttIntegration
{
    public const string SwitchVoiceEnable = "camera_motion_detect_alarm_voice_enable";
    public const string SwitchMailEnable = "camera_motion_detect_alarm_mail_enable";
    public const string SwitchLightEnable = "camera_light_enable";

    public const string BinarySensorMotionDetectedAlarm = "camera_motion_detected_alarm";
    public const string BinarySensorIsProblemDetected = "camera_is_problem_detected";

    public const string CameraRestartButton = "camera_restart";

    public const string CameraSnapshot = "camera_snapshot";

    string ClientId { get; }

    string GetStatusTopic();

    string GetStatusTopic(string deviceName);

    string GetCommandTopic(string deviceName, string command);

    string GetButtonTopic(string deviceName, string button);

    string GetStateTopic(string deviceName);

    string GetStatePayload(DvrState state);

    string GetCameraSnapshotTopic(string deviceName);

    string[] GetSwitches(DvrOptions options);

    string[] GetBinarySensors(DvrOptions options);

    string[] GetButtons(DvrOptions _);

    string GetDescription(string entity);
}