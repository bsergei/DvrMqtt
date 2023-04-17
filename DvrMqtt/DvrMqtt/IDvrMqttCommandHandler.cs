namespace DvrMqtt.DvrMqtt;

public interface IDvrMqttCommandHandler
{
    void ConnectEvents(IDvrMqttEvents dvrMqttEvents);
}