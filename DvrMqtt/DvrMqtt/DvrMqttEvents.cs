using System.Reactive.Linq;
using System.Reactive.Subjects;
using Dvr;
using Mqtt;

namespace DvrMqtt.DvrMqtt;

public class DvrMqttEvents : IDvrMqttEvents, IDisposable
{
    private readonly ISubject<IDvrIpWebService?> dvrConnections_ = Subject.Synchronize(new Subject<IDvrIpWebService?>());
    private readonly ISubject<IMqttClientService?> mqttConnections_ = Subject.Synchronize(new Subject<IMqttClientService?>());
    private readonly ISubject<(Action<DvrState>, Task)> stateUpdateRequests_ = Subject.Synchronize(new Subject<(Action<DvrState>, Task)>());
    private readonly ISubject<Void> unhealthyUpdates_ = Subject.Synchronize(new Subject<Void>());

    private CancellationTokenSource? cts_;
    private List<IDisposable>? cancellationRegistrations_;

    public DvrMqttEvents()
    {
        cts_ = new CancellationTokenSource();
        cancellationRegistrations_ = new List<IDisposable>();
    }

    public void Dispose()
    {
        var registrations = Interlocked.Exchange(ref cancellationRegistrations_, null);
        foreach (var registration in registrations ?? Enumerable.Empty<IDisposable>())
        {
            registration.Dispose();
        }

        using var cts = Interlocked.Exchange(ref cts_, null);
        cts?.Cancel();
    }

    public CancellationToken StoppingToken => cts_?.Token ?? throw new ObjectDisposedException("DvrMqttEvents disposed");

    public ISubject<IMqttClientService?> MqttConnections => mqttConnections_;

    public ISubject<IDvrIpWebService?> DvrConnections => dvrConnections_;

    public ISubject<(Action<DvrState>, Task)> StateUpdateRequests => stateUpdateRequests_;

    public ISubject<Void> Unhealthy => unhealthyUpdates_;

    public IObservable<(IDvrIpWebService dvr, IMqttClientService mqtt)> DvrAndMqttConnected =>
        DvrConnections
            .CombineLatest(MqttConnections, (dvr, mqttClient) => (dvr, mqttClient))
            .Where(_ => _.dvr != null && _.mqttClient != null)
            .Select(_ => (_.dvr!, _.mqttClient!));

    public IObservable<IMqttClientService> DvrDisconnected =>
        DvrConnections
            .CombineLatest(
                MqttConnections,
                (dvr, mqtt) => (dvr, mqtt))
            .Where(_ => _.dvr == null && _.mqtt != null)
            .Select(_ => _.mqtt!);

    public IObservable<Void> DvrOrMqttDisconnected =>
        DvrConnections
            .Where(_ => _ == null).Select(_ => Void.Value)
            .Concat(
                MqttConnections.Where(_ => _ == null).Select(_ => Void.Value));

    public void LinkStoppingToken(CancellationToken cancellationToken)
    {
        cancellationRegistrations_?.Add(cancellationToken.Register(Cancel));
    }

    private void Cancel()
    {
        mqttConnections_.OnCompleted();
        mqttConnections_.OnCompleted();
        dvrConnections_.OnCompleted();
        stateUpdateRequests_.OnCompleted();
        unhealthyUpdates_.OnCompleted();

        cts_?.Cancel();
    }
}