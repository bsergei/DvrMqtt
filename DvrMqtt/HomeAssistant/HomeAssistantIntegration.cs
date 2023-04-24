using Dvr;
using DvrMqtt.DvrMqtt;
using HomeAssistant.Model;
using Microsoft.Extensions.Options;
using Mqtt;

namespace DvrMqtt.HomeAssistant;

public class HomeAssistantIntegration : IHomeAssistantIntegration
{
    private readonly DvrOptions options_;
    private readonly DvrMqttOptions dvrMqttOptions_;
    private readonly IMqttIntegration mqttIntegration_;
    private readonly IHomeAssistant homeAssistant_;
    private readonly string dvrName_;

    public HomeAssistantIntegration(
        IOptions<DvrMqttOptions> dvrMqttOptions,
        IOptions<DvrOptions> dvrOptions,
        IMqttIntegration mqttIntegration,
        IHomeAssistant homeAssistant)
    {
        dvrMqttOptions_ = dvrMqttOptions.Value;
        mqttIntegration_ = mqttIntegration;
        homeAssistant_ = homeAssistant;
        options_ = dvrOptions.Value;
        dvrName_ = dvrOptions.Value.UniqueId;
    }

    public async Task PublishAutoDiscoverInformation(IMqttClientService mqttClient)
    {
        if (homeAssistant_.IsEnabled)
        {
            foreach (var commandId in mqttIntegration_.GetSwitches(options_))
            {
                await PublishSwitchConfig(mqttClient, commandId);
            }

            foreach (var binarySensor in mqttIntegration_.GetBinarySensors(options_))
            {
                await PublishBinarySensorConfig(
                    mqttClient, 
                    binarySensor, 
                    GetBinarySensorDeviceClass(binarySensor));
            }

            if (dvrMqttOptions_.SnapshotsUpdateInterval > 0 && !options_.IsNvr)
            {
                await PublishCameraSnapshotConfig(mqttClient);
            }

            foreach (var button in mqttIntegration_.GetButtons(options_))
            {
                await PublishButtonConfig(
                    mqttClient, 
                    button, 
                    GetButtonDeviceClass(button));
            }
        }
    }

    private static string? GetBinarySensorDeviceClass(string binarySensor)
    {
        string? deviceClass;
        switch (binarySensor)
        {
            case IMqttIntegration.BinarySensorMotionDetectedAlarm:
                deviceClass = BinarySensorDeviceClasses.Motion;
                break;

            case IMqttIntegration.BinarySensorIsProblemDetected:
                deviceClass = BinarySensorDeviceClasses.Problem;
                break;
            default:
                deviceClass = null;
                break;
        }

        return deviceClass;
    }

    private static string? GetButtonDeviceClass(string button)
    {
        string? deviceClass;
        switch (button)
        {
            case IMqttIntegration.CameraRestartButton:
                deviceClass = ButtonDeviceClasses.DeviceClassRestart;
                break;
            default:
                deviceClass = null;
                break;
        }

        return deviceClass;
    }

    private async Task PublishSwitchConfig(IMqttClientService mqttClient, string command)
    {
        var (payload, topic) = homeAssistant_.GetSwitchAutoDiscovery(
            dvrName_,
            command,
            EntityCategories.EntityCategoryConfig);

        await mqttClient.Publish(topic, payload, true);
    }

    private async Task PublishBinarySensorConfig(IMqttClientService mqttClient, string sensorName, string? deviceClass = null)
    {
        var (payload, topic) = homeAssistant_.GetBinarySensorAutoDiscovery(
            dvrName_,
            sensorName,
            deviceClass);

        await mqttClient.Publish(topic, payload, true);
    }

    private async Task PublishCameraSnapshotConfig(IMqttClientService mqttClient)
    {
        var (payload, topic) = homeAssistant_.GetCameraAutoDiscovery(dvrName_);
        await mqttClient.Publish(topic, payload, true);
    }

    private async Task PublishButtonConfig(IMqttClientService mqttClient, string button, string? deviceClass = null)
    {
        var (payload, topic) = homeAssistant_.GetButtonAutoDiscovery(dvrName_, button, deviceClass);
        await mqttClient.Publish(topic, payload, true);
    }
}