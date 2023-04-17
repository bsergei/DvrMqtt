namespace Mqtt;

public interface IMqttClientService
{
    Task Connect(string clientId, string willTopic, string willPayload);

    Task Publish(string topic, string payload, bool retain = false, int qos = 0);

    IObservable<string> ObserveTopic(string topic);

    IObservable<bool?> ObserveConnectionState();
}