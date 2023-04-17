using Mqtt;

namespace DvrMqtt.HomeAssistant;

public interface IHomeAssistantIntegration
{
    Task PublishAutoDiscoverInformation(IMqttClientService mqttClient);
}