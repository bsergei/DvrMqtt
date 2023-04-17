using Dvr;
using HomeAssistant.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mqtt;

namespace DvrMqtt.DvrMqtt;

public class DvrMqttService : IDvrMqttService, IDisposable
{
    private readonly DvrMqttOptions options_;
    private readonly DvrOptions dvrOptions_;
    private readonly IMqttIntegration mqttIntegration_;
    private readonly Func<IDvrIpWebService> dvrWebIpServiceFactory_;
    private readonly IMqttClientService mqttClientService_;
    private readonly ILogger<DvrMqttService> logger_;
    private readonly IDvrMqttEvents dvrMqttEvents_;
    private readonly IDvrMqttStatePublisher dvrMqttStatePublisher_;
    private readonly IDvrMqttCommandHandler dvrMqttCommandHandler_;
    
    private CancellationTokenSource? disposeCancellationToken_;

    public DvrMqttService(
        IOptions<DvrOptions> dvrOptions,
        IOptions<DvrMqttOptions> options,
        IMqttIntegration mqttIntegration,
        Func<IDvrIpWebService> dvrWebIpServiceFactory,
        IMqttClientService mqttClientService,
        ILogger<DvrMqttService> logger,
        IDvrMqttEvents dvrMqttEvents,
        IDvrMqttStatePublisher dvrMqttStatePublisher,
        IDvrMqttCommandHandler dvrMqttCommandHandler)
    {
        if (String.IsNullOrWhiteSpace(options.Value.TopicBase))
        {
            throw new ArgumentException("TopicBase should not be empty", nameof(options.Value.TopicBase));
        }

        if (String.IsNullOrWhiteSpace(dvrOptions.Value.UniqueId))
        {
            throw new ArgumentException("UniqueId should not be empty", nameof(dvrOptions.Value.UniqueId));
        }

        if (String.IsNullOrWhiteSpace(dvrOptions.Value.HostIp))
        {
            throw new ArgumentException("HostIp should not be empty", nameof(dvrOptions.Value.HostIp));
        }

        options_ = options.Value;
        dvrOptions_ = dvrOptions.Value;
        mqttIntegration_ = mqttIntegration;
        dvrWebIpServiceFactory_ = dvrWebIpServiceFactory;
        mqttClientService_ = mqttClientService;
        logger_ = logger;
        dvrMqttEvents_ = dvrMqttEvents;
        dvrMqttStatePublisher_ = dvrMqttStatePublisher;
        dvrMqttCommandHandler_ = dvrMqttCommandHandler;
    }

    public void Dispose()
    {
        using var cts = Interlocked.Exchange(ref disposeCancellationToken_, null);
        cts?.Cancel();
    }

    public async Task Start(CancellationToken stoppingToken)
    {
        try
        {
            disposeCancellationToken_ = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            var cancellationToken = stoppingToken;

            dvrMqttEvents_.LinkStoppingToken(cancellationToken);

            dvrMqttStatePublisher_.ConnectEvents(dvrMqttEvents_);
            dvrMqttCommandHandler_.ConnectEvents(dvrMqttEvents_);

            mqttClientService_.ObserveConnectionState()
                .Subscribe(state =>
                    {
                        if (state == true)
                        {
                            dvrMqttEvents_.MqttConnections.OnNext(mqttClientService_);
                        }
                        else if (state == false)
                        {
                            dvrMqttEvents_.MqttConnections.OnNext(null);
                        }
                    },
                    error =>
                    {
                        logger_.LogError(error, "MQTT connection error");
                    },
                    cancellationToken);

            await mqttClientService_.Connect(
                $"{options_.ClientId}_{dvrOptions_.UniqueId}",
                mqttIntegration_.GetStatusTopic(dvrOptions_.UniqueId),
                Defaults.StatusOffline);

            Contingency.Run(dvrWebIpServiceFactory_)
                .Subscribe(
                    dvr =>
                    {
                        if (dvr is null)
                        {
                            logger_.LogInformation("DVR disconnected");
                        }
                        else
                        {
                            logger_.LogInformation("DVR connected");
                        }

                        dvrMqttEvents_.DvrConnections.OnNext(dvr);
                    },
                    cancellationToken);
        }
        catch (Exception exception)
        {
            logger_.LogCritical(exception, "Run method finished with unhandled error");
        }
    }
}