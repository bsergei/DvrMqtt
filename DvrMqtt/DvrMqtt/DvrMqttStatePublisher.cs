using System.Reactive.Linq;
using Dvr;
using Dvr.Commands;
using DvrMqtt.HomeAssistant;
using HomeAssistant.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mqtt;

namespace DvrMqtt.DvrMqtt;

public class DvrMqttStatePublisher : IDisposable, IDvrMqttStatePublisher
{
    private readonly DvrOptions dvrOptions_;
    private readonly DvrMqttOptions dvrMqttOptions_;
    private readonly IDvrHttpWebService dvrHttpWeb_;
    private readonly IMqttIntegration mqttIntegration_;
    private readonly IHomeAssistantIntegration homeAssistantIntegration_;
    private readonly ILogger<DvrMqttStatePublisher> logger_;

    private readonly DvrState state_ = new();

    private IObserver<Void>? unhealthy_;

    public DvrMqttStatePublisher(
        IOptions<DvrOptions> dvrOptions,
        IOptions<DvrMqttOptions> dvrMqttOptions,
        IDvrHttpWebService dvrHttpWeb, 
        IMqttIntegration mqttIntegration,
        IHomeAssistantIntegration homeAssistantIntegration,
        ILogger<DvrMqttStatePublisher> logger)
    {
        dvrOptions_ = dvrOptions.Value;
        dvrMqttOptions_ = dvrMqttOptions.Value;
        dvrHttpWeb_ = dvrHttpWeb;
        mqttIntegration_ = mqttIntegration;
        homeAssistantIntegration_ = homeAssistantIntegration;
        logger_ = logger;
    }

    public void ConnectEvents(IDvrMqttEvents dvrMqttEvents)
    {
        unhealthy_ = dvrMqttEvents.Unhealthy;

        dvrMqttEvents.DvrAndMqttConnected
            .Subscribe(_ =>
            {
                RunStatePublishers(
                        _.dvr, 
                        _.mqtt, 
                        dvrMqttEvents.DvrOrMqttDisconnected, 
                        dvrMqttEvents.StoppingToken)
                    .Forget();
            }, dvrMqttEvents.StoppingToken);

        dvrMqttEvents.DvrDisconnected
            .Subscribe(mqtt =>
            {
                SetDvrStatus(mqtt, false)
                    .Forget();
            }, dvrMqttEvents.StoppingToken);

        dvrMqttEvents.MqttConnections
            .Where(mqtt => mqtt != null)
            .Select(mqtt => mqtt!)
            .Subscribe(mqtt =>
            {
                PublishAutoDiscovery(mqtt)
                    .Forget();
            }, dvrMqttEvents.StoppingToken);

        dvrMqttEvents.MqttConnections
            .Where(mqtt => mqtt != null)
            .Select(mqtt => mqtt!)
            .SelectMany(mqtt => dvrMqttEvents.Unhealthy
                .Window(TimeSpan.FromSeconds(60))
                .SelectMany(_ => _.Take(1).Select(__ => mqtt)))
            .Subscribe(mqtt =>
            {
                UpdateAndPublishIsProblemDetectedState(mqtt, true)
                    .Forget();
            }, dvrMqttEvents.StoppingToken);

        dvrMqttEvents.MqttConnections
            .Where(mqtt => mqtt != null)
            .Select(mqtt => mqtt!)
            .SelectMany(mqtt => dvrMqttEvents.Unhealthy
                .Throttle(TimeSpan.FromSeconds(60))
                .Select(_ => (mqtt)))
            .Subscribe(mqtt =>
            {
                // Clear problem state.
                UpdateAndPublishIsProblemDetectedState(mqtt, false)
                    .Forget();
            }, dvrMqttEvents.StoppingToken);

        dvrMqttEvents.DvrAndMqttConnected
            .CombineLatest(dvrMqttEvents.StateUpdateRequests, (tuple, state) => (tuple.dvr, tuple.mqtt, state))
            .Subscribe(_ =>
            {
                UpdateAndPublishState(_.dvr, _.mqtt, _.state)
                    .Forget();
            }, dvrMqttEvents.StoppingToken);
    }

    public void Dispose()
    {
    }

    private async Task RunStatePublishers(
        IDvrIpWebService dvr, 
        IMqttClientService mqtt,  
        IObservable<Void> dvrOrMqttDisconnected,
        CancellationToken cancellationToken)
    {
        try
        {
            logger_.LogInformation("State publishers started");

            await SetDvrStatus(mqtt, true);

            UpdateStateMotionDetect(false);
            await UpdateStateConfigProps(dvr);
            await PublishState(mqtt);

            var taskUpdateConfigProps = RunPublishConfigurationProperties(dvr, mqtt, dvrOrMqttDisconnected, cancellationToken);

            var snapshotUpdate = dvrMqttOptions_.SnapshotsUpdateInterval <= 0 || dvrOptions_.IsNvr
                ? Task.CompletedTask
                : RunPublishDvrSnapshots(mqtt, dvrOrMqttDisconnected, cancellationToken);

            var taskObserveAlarms =
                dvrOptions_.IsNvr
                    ? Task.CompletedTask
                    : RunPublishMotionDetectEvents(dvr, mqtt, dvrOrMqttDisconnected, cancellationToken);

            await Task.WhenAll(
                taskUpdateConfigProps,
                snapshotUpdate,
                taskObserveAlarms);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            SetUnhealthy();
            logger_.LogError("RunStatePublishers failed");
        }
        finally
        {
            logger_.LogInformation("State publishers finished");
        }
    }

    private Task RunPublishConfigurationProperties(
        IDvrIpWebService dvr, 
        IMqttClientService mqtt,  
        IObservable<Void> dvrOrMqttDisconnected,
        CancellationToken cancellationToken)
    {
        return Observable
            .Interval(TimeSpan.FromSeconds(60))
            .TakeUntil(dvrOrMqttDisconnected)
            .SelectMany(async _ =>
            {
                try
                {
                    await UpdateAndPublishState(dvr, mqtt, null);
                }
                catch (Exception e)
                {
                    SetUnhealthy();
                    logger_.LogError(e, "PublishConfigurationProperties failed");
                }

                return _;
            })
            .ForEachAsync(_ => { }, cancellationToken);
    }

    private Task RunPublishDvrSnapshots(
        IMqttClientService mqtt,  
        IObservable<Void> dvrOrMqttDisconnected,
        CancellationToken cancellationToken)
    {
        return Observable
            .Interval(TimeSpan.FromSeconds(dvrMqttOptions_.SnapshotsUpdateInterval))
            .TakeUntil(dvrOrMqttDisconnected)
            .SelectMany(async _ =>
            {
                try
                {
                    await SendSnapshot(mqtt);
                }
                catch (Exception e)
                {
                    SetUnhealthy();
                    logger_.LogError(e, "PublishDvrSnapshots failed");
                }

                return _;
            })
            .ForEachAsync(_ => { }, cancellationToken);
    }

    private Task RunPublishMotionDetectEvents(
        IDvrIpWebService dvr, 
        IMqttClientService mqtt, 
        IObservable<Void> dvrOrMqttDisconnected,
        CancellationToken cancellationToken)
    {
        return dvr
            .ObserveAlarms()
            .TakeUntil(dvrOrMqttDisconnected)
            .SelectMany(async alarmInfo =>
            {
                try
                {
                    logger_.LogInformation($"Motion alarm: {alarmInfo.Event}-{alarmInfo.Status}");
                    if (alarmInfo.Event == AlarmInfo.MotionDetectEvent)
                    {
                        var isStart = alarmInfo.Status == AlarmInfo.StatusStart;
                        UpdateStateMotionDetect(isStart);
                        await PublishState(mqtt);
                    }
                }
                catch (Exception e)
                {
                    SetUnhealthy();
                    logger_.LogError(e, "PublishMotionDetectEvents failed");
                }

                return Void.Value;
            })
            .ForEachAsync(_ => { }, cancellationToken);
    }

    private async Task SetDvrStatus(IMqttClientService mqtt, bool online)
    {
        await mqtt.Publish(
            mqttIntegration_.GetStatusTopic(dvrOptions_.UniqueId),
            online
                ? Defaults.StatusOnline
                : Defaults.StatusOffline,
            retain: true);
    }

    private async Task UpdateAndPublishState(IDvrIpWebService dvr, IMqttClientService mqtt, (Action<DvrState>, Task)? state)
    {
        if (state != null)
        {
            // Update state immediately.
            state.Value.Item1(state_);
            await PublishState(mqtt);

            try
            {
                // Wait task that wil change state on device.
                await (state.Value.Item2).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Handled in requester.
            }
        }

        await UpdateStateConfigProps(dvr);
        await PublishState(mqtt);
    }

    private void UpdateStateMotionDetect(bool isDetected)
    {
        state_.MotionDetectedAlarm = isDetected
            ? Defaults.PayloadOn
            : Defaults.PayloadOff;
    }

    private async Task UpdateStateConfigProps(IDvrIpWebService? dvr)
    {
        if (dvr == null)
        {
            return;
        }

        var md = await dvr.GetMotionDetectConfiguration();

        state_.VoiceEnable = md.VoiceEnable ? Defaults.PayloadOn : Defaults.PayloadOff;
        state_.MailEnable = md.MailEnable ? Defaults.PayloadOn : Defaults.PayloadOff;

        if (!dvrOptions_.IsNvr)
        {
            var cp = await dvr.GetCameraParam();
            var light = cp.DayNightColor == DvrIpWebService.DayNightColorFull;

            state_.CameraLight = light ? Defaults.PayloadOn : Defaults.PayloadOff;
        }
    }

    private async Task PublishState(IMqttClientService mqtt)
    {
        var stateTopic = mqttIntegration_.GetStateTopic(dvrOptions_.UniqueId);
        var statePayload = mqttIntegration_.GetStatePayload(state_);
        await mqtt.Publish(stateTopic, statePayload);
    }

    private async Task SendSnapshot(IMqttClientService mqtt)
    {
        var topic = mqttIntegration_.GetCameraSnapshotTopic(dvrOptions_.UniqueId);
        var snapshot = await dvrHttpWeb_.GetCameraSnapshot();
        var payload = Convert.ToBase64String(snapshot);
        await mqtt.Publish(topic, payload);
    }

    private async Task UpdateAndPublishIsProblemDetectedState(IMqttClientService mqtt, bool isProblem)
    {
        UpdateIsProblemDetectedState(isProblem);
        await PublishState(mqtt);
    }

    private void UpdateIsProblemDetectedState(bool isProblem)
    {
        state_.IsProblemDetected = isProblem 
            ? Defaults.PayloadOn 
            : Defaults.PayloadOff;
    }

    private async Task PublishAutoDiscovery(IMqttClientService mqtt)
    {
        try
        {
            await homeAssistantIntegration_.PublishAutoDiscoverInformation(mqtt);
        }
        catch (Exception e)
        {
            logger_.LogError(e, "PublishAutoDiscoverInformation failed");
            throw;
        }
    }

    private void SetUnhealthy()
    {
        unhealthy_?.OnNext(default);
    }
}