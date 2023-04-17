using System.Reactive.Subjects;
using Dvr;
using Mqtt;

namespace DvrMqtt.DvrMqtt;

public interface IDvrMqttEvents
{
    CancellationToken StoppingToken { get; }

    ISubject<IMqttClientService?> MqttConnections { get; }

    ISubject<IDvrIpWebService?> DvrConnections { get; }

    ISubject<(Action<DvrState>, Task)> StateUpdateRequests { get; }

    ISubject<Void> Unhealthy { get; }

    IObservable<(IDvrIpWebService dvr, IMqttClientService mqtt)> DvrAndMqttConnected { get; }

    IObservable<IMqttClientService> DvrDisconnected { get; }

    IObservable<Void> DvrOrMqttDisconnected { get; }

    void LinkStoppingToken(CancellationToken cancellationToken);
}