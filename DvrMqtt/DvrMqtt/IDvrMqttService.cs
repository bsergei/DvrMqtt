namespace DvrMqtt.DvrMqtt;

public interface IDvrMqttService
{
    Task Start(CancellationToken stoppingToken);
}