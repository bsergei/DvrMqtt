using System.Text.Json;
using DvrMqtt.DvrMqtt;
using HomeAssistant.Model;
using Microsoft.Extensions.Options;

namespace DvrMqtt.HomeAssistant;

public class HomeAssistant : IHomeAssistant
{
    private readonly IMqttIntegration integration_;
    private readonly JsonSerializerOptions jsonSerializerOptions_;
    private readonly HomeAssistantOptions options_;

    public HomeAssistant(
        IMqttIntegration integration,
        JsonSerializerOptions jsonSerializerOptions,
        IOptions<HomeAssistantOptions> options)
    {
        integration_ = integration;
        jsonSerializerOptions_ = jsonSerializerOptions;
        options_ = options.Value;

        ValidateOptions();
    }
    
    public bool IsEnabled => options_.IsEnabled ?? false;

    public (string Payload, string Topic) GetSwitchAutoDiscovery(string deviceName, string command, string entityCategory)
    {
        var config = new SwitchConfig
        {
            Availability = new[]
            {
                new Availability { Topic = integration_.GetStatusTopic(deviceName) }
            },
            CommandTopic = integration_.GetCommandTopic(deviceName, command),
            Device = new Device
            {
                Identifiers = new[] { $"{integration_.ClientId.ToLower()}_{deviceName}" },
                Manufacturer = "China",
                Model = "Generic XM Camera",
                Name = $"{deviceName}"
            },
            EntityCategory = entityCategory,
            Icon = "mdi:toggle-switch",
            //JsonAttributesTopic = $"{integration_.GetStateTopic(deviceName)}",
            Name = GetDeviceConfigEntityName(deviceName, command),
            StateTopic = integration_.GetStateTopic(deviceName),
            UniqueID = $"{integration_.ClientId.ToLower()}_{deviceName.ToLower()}_{command.ToLower()}",
            ValueTemplate = $"{{{{ value_json.{command} }}}}"
        };

        return (
            JsonSerializer.Serialize(config, jsonSerializerOptions_),
            GetSwitchConfigTopic(deviceName, command)
        );
    }

    public (string Payload, string Topic) GetBinarySensorAutoDiscovery(string deviceName, string sensorName, string? deviceClass)
    {
        var config = new BinarySensorConfig
        {
            Availability = new[]
            {
                new Availability { Topic = integration_.GetStatusTopic(deviceName) }
            },
            Device = new Device
            {
                Identifiers = new[] { $"{integration_.ClientId.ToLower()}_{deviceName}" },
                Manufacturer = "China",
                Model = "Generic XM Camera",
                Name = $"{deviceName}"
            },
            Name = GetDeviceConfigSensorName(deviceName, sensorName),
            StateTopic = integration_.GetStateTopic(deviceName),
            UniqueID = $"{integration_.ClientId.ToLower()}_{deviceName.ToLower()}_{sensorName.ToLower()}",
            ObjectID = $"{deviceName.ToLower()}_{sensorName.ToLower()}",
            ValueTemplate = $"{{{{ value_json.{sensorName} }}}}"
        };

        if (deviceClass != null)
        {
            config.DeviceClass = deviceClass;
        }

        return (
            JsonSerializer.Serialize(config, jsonSerializerOptions_),
            GetBinarySensorConfigTopic(deviceName, sensorName)
        );
    }

    public (string Payload, string Topic) GetCameraAutoDiscovery(string deviceName)
    {
        var config = new CameraConfig
        {
            Availability = new[]
            {
                new Availability { Topic = integration_.GetStatusTopic(deviceName) }
            },
            Device = new Device
            {
                Identifiers = new[] { $"{integration_.ClientId.ToLower()}_{deviceName}" },
                Manufacturer = "China",
                Model = "Generic XM Camera",
                Name = $"{deviceName}"
            },
            Name = GetDeviceConfigCameraName(deviceName),
            UniqueID = $"{integration_.ClientId.ToLower()}_{deviceName.ToLower()}_{IMqttIntegration.CameraSnapshot.ToLower()}",
            Topic = integration_.GetCameraSnapshotTopic(deviceName),
            //JsonAttributesTopic = integration_.GetStateTopic(deviceName)
        };

        return (
            JsonSerializer.Serialize(config, jsonSerializerOptions_),
            GetCameraConfigTopic(deviceName)
        );
    }

    public (string Payload, string Topic) GetButtonAutoDiscovery(string deviceName, string button, string? deviceClass = null)
    {
        var config = new ButtonConfig
        {
            Availability = new[]
            {
                new Availability { Topic = integration_.GetStatusTopic(deviceName) }
            },
            Device = new Device
            {
                Identifiers = new[] { $"{integration_.ClientId.ToLower()}_{deviceName}" },
                Manufacturer = "China",
                Model = "Generic XM Camera",
                Name = $"{deviceName}"
            },
            
            DeviceClass = deviceClass ?? ButtonDeviceClasses.DeviceClassNone,
            Name = GetDeviceConfigButtonName(deviceName, button),
            CommandTopic = integration_.GetButtonTopic(deviceName, button),
            UniqueID = $"{integration_.ClientId.ToLower()}_{deviceName.ToLower()}_{button.ToLower()}",
            ObjectID = $"{deviceName.ToLower()}_{button.ToLower()}"
            
        };

        return (
            JsonSerializer.Serialize(config, jsonSerializerOptions_),
            GetButtonConfigTopic(deviceName, button)
        );
    }

    private string GetBinarySensorConfigTopic(string deviceName, string sensorName) =>
        String.Join(
            "/",
            options_.HomeAssistantAutoDiscoveryTopicBase,
            "binary_sensor",
            deviceName,
            sensorName,
            "config");

    private string GetSwitchConfigTopic(string deviceName, string command) =>
        String.Join(
            "/",
            options_.HomeAssistantAutoDiscoveryTopicBase,
            "switch",
            deviceName,
            command,
            "config");

    private string GetCameraConfigTopic(string deviceName) =>
        String.Join(
            "/",
            options_.HomeAssistantAutoDiscoveryTopicBase,
            "camera",
            deviceName,
            IMqttIntegration.CameraSnapshot,
            "config");

    private string GetButtonConfigTopic(string deviceName, string button)=>
        String.Join(
            "/",
            options_.HomeAssistantAutoDiscoveryTopicBase,
            "button",
            deviceName,
            button,
            "config");

    private void ValidateOptions()
    {
        if (options_.IsEnabled == true)
        {
            if (string.IsNullOrWhiteSpace(options_.HomeAssistantAutoDiscoveryTopicBase))
            {
                throw new ArgumentException(
                    "Parameter should not be empty",
                    nameof(HomeAssistantOptions.HomeAssistantAutoDiscoveryTopicBase));
            }
        }
    }

    private string GetDeviceConfigEntityName(string deviceName, string command) =>
        string.Join(" ", deviceName, integration_.GetDescription(command));

    private string GetDeviceConfigSensorName(string deviceName, string sensorName) =>
        string.Join(" ", deviceName, integration_.GetDescription(sensorName));

    private string GetDeviceConfigCameraName(string deviceName) =>
        string.Join(" ", deviceName, integration_.GetDescription(IMqttIntegration.CameraSnapshot));

    private string GetDeviceConfigButtonName(string deviceName, string button) =>
        string.Join(" ", deviceName, integration_.GetDescription(button));
}