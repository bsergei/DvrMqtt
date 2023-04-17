using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;

namespace Mqtt;

public class MqttClientService : IDisposable, IMqttClientService
{
    private readonly IOptions<MqttServerOptions> options_;
    private IManagedMqttClient? client_;
    private readonly MqttFactory mqttFactory_;

    private readonly Dictionary<string, List<IObserver<string>>> observers_ = new();
    private readonly ISubject<bool?> connectionStateSubject_ = new BehaviorSubject<bool?>(null);

    public MqttClientService(IOptions<MqttServerOptions> options)
    {
        if (String.IsNullOrWhiteSpace(options.Value.ConnectionUri))
        {
            throw new ArgumentException("ConnectionUri should not be empty", nameof(options.Value.ConnectionUri));
        }

        options_ = options;

        mqttFactory_ = new MqttFactory();
    }

    public void Dispose()
    {
        CompleteAllObservers();
        client_?.Dispose();
    }

    public async Task Connect(string clientId, string willTopic, string willPayload)
    {
        var options = options_.Value;
        var managedMqttClientOptions = new ManagedMqttClientOptionsBuilder()
            .WithClientOptions(
                new MqttClientOptionsBuilder()
                    .WithClientId(clientId)
                    .WithCredentials(options.Username, options.Password)
                    .WithConnectionUri(new Uri(options.ConnectionUri))
                    .WithWillTopic(willTopic)
                    .WithWillPayload(willPayload)
                    .WithWillRetain()
                    .Build())
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(10))
            .Build();

        client_ = mqttFactory_.CreateManagedMqttClient();
        client_.ApplicationMessageReceivedAsync += OnApplicationMessageReceivedAsync;
        client_.ConnectionStateChangedAsync += OnConnectionStateChangedAsync;

        await client_.StartAsync(managedMqttClientOptions);
    }

    public async Task Publish(string topic, string payload, bool retain = false, int qos = 0)
    {
        if (client_ == null)
        {
            throw new InvalidOperationException("MQTT not connected");
        }

        await client_.EnqueueAsync(topic, payload, (MqttQualityOfServiceLevel)qos, retain);
    }

    public IObservable<string> ObserveTopic(string topic)
    {
        if (client_ == null)
        {
            throw new InvalidOperationException("MQTT not connected");
        }

        return Observable.Create<string>(async observer =>
        {
            AddTopicObserver(topic, observer);
            await client_.SubscribeAsync(topic);
            
            return () =>
            {
                client_.UnsubscribeAsync(topic);
                RemoveTopicObserver(topic, observer);
            };
        });
    }

    public IObservable<bool?> ObserveConnectionState()
    {
        return connectionStateSubject_;
    }

    private void AddTopicObserver(string topic, IObserver<string> observer)
    {
        lock (observers_)
        {
            if (!observers_.TryGetValue(topic, out var observers))
            {
                observers = new List<IObserver<string>>();
                observers_[topic] = observers;
            }

            observers.Add(observer);
        }
    }

    private void RemoveTopicObserver(string topic, IObserver<string> observer)
    {
        lock (observers_)
        {
            if (observers_.TryGetValue(topic, out var observers))
            {
                observers.Remove(observer);
            }

            if (observers?.Count == 0)
            {
                observers_.Remove(topic);
            }
        }
    }

    private IObserver<string>[] GetTopicObservers(string topic)
    {
        lock (observers_)
        {
            if (observers_.TryGetValue(topic, out var observers))
            {
                // Returns copy to process out of locking context.
                // It can be unsafe, but dead-lock free.
                return observers.ToArray();
            }

            return Array.Empty<IObserver<string>>();
        }
    }

    private void CompleteAllObservers()
    {
        lock (observers_)
        {
            foreach (var observerKvp in observers_)
            {
                foreach (var observer in observerKvp.Value)
                {
                    observer.OnCompleted();
                }
            }

            observers_.Clear();
        }
    }

    private Task OnConnectionStateChangedAsync(EventArgs arg)
    {
        connectionStateSubject_.OnNext(client_?.IsConnected);
        return Task.CompletedTask;
    }

    private Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        foreach (var observer in GetTopicObservers(e.ApplicationMessage.Topic))
        {
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            observer.OnNext(payload);
        }

        return Task.CompletedTask;
    }
}