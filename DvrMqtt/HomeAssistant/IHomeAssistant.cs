namespace DvrMqtt.HomeAssistant;

public interface IHomeAssistant
{
    bool IsEnabled { get; }

    (string Payload, string Topic) GetSwitchAutoDiscovery(
        string deviceName, 
        string command, 
        string entityCategory);

    (string Payload, string Topic) GetBinarySensorAutoDiscovery(
        string deviceName, 
        string sensorName,
        string? deviceClass);

    (string Payload, string Topic) GetCameraAutoDiscovery(string deviceName);

    (string Payload, string Topic) GetButtonAutoDiscovery(
        string deviceName, 
        string button, 
        string? deviceClass = null);
}