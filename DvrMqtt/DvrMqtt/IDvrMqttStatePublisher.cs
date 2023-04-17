namespace DvrMqtt.DvrMqtt;

public interface IDvrMqttStatePublisher
{
    void ConnectEvents(IDvrMqttEvents dvrMqttEvents);
}