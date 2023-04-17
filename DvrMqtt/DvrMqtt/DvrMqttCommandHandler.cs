using System.Diagnostics;
using System.Reactive.Linq;
using Dvr;
using HomeAssistant.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mqtt;

namespace DvrMqtt.DvrMqtt;

public class DvrMqttCommandHandler : IDvrMqttCommandHandler
{
    private readonly DvrOptions dvrOptions_;
    private readonly IMqttIntegration mqttIntegration_;
    private readonly ILogger<DvrMqttCommandHandler> logger_;
    private IObserver<Void>? unhealthy_;

    public DvrMqttCommandHandler(
        IOptions<DvrOptions> dvrOptions,
        IMqttIntegration mqttIntegration,
        ILogger<DvrMqttCommandHandler> logger)
    {
        mqttIntegration_ = mqttIntegration;
        logger_ = logger;
        dvrOptions_ = dvrOptions.Value;
    }

    public void ConnectEvents(IDvrMqttEvents dvrMqttEvents)
    {
        unhealthy_ = dvrMqttEvents.Unhealthy;

        dvrMqttEvents.DvrAndMqttConnected
            .SelectMany(async _ =>
            {
                try
                {
                    await RunCommandHandlers(
                        _.dvr, 
                        _.mqtt, 
                        dvrMqttEvents.DvrOrMqttDisconnected,
                        dvrMqttEvents.StateUpdateRequests,
                        dvrMqttEvents.StoppingToken);
                }
                catch (Exception e)
                {
                    logger_.LogError(e, "Error in RunCommandHandlers");
                }

                return Void.Value;
            })
            .ForEachAsync(_ => { }, dvrMqttEvents.StoppingToken);
    }

    private async Task RunCommandHandlers(
        IDvrIpWebService dvr, 
        IMqttClientService mqtt, 
        IObservable<Void> dvrOrMqttDisconnected,
        IObserver<(Action<DvrState>, Task)> stateUpdateRequests,
        CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();

        foreach (var commandId in mqttIntegration_.GetSwitches(dvrOptions_))
        {
            var commandHandlerTask = RunCommandHandler(
                dvr, 
                mqtt, 
                commandId, 
                dvrOrMqttDisconnected,
                stateUpdateRequests,
                cancellationToken);

            tasks.Add(commandHandlerTask);
        }

        foreach (var button in mqttIntegration_.GetButtons(dvrOptions_))
        {
            var buttonHandler = RunButtonHandler(
                dvr,
                mqtt,
                button,
                dvrOrMqttDisconnected,
                cancellationToken);

            tasks.Add(buttonHandler);
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RunCommandHandler(
        IDvrIpWebService dvr,
        IMqttClientService mqtt,
        string command,
        IObservable<Void> dvrOrMqttDisconnected,
        IObserver<(Action<DvrState>, Task)> stateUpdateRequests,
        CancellationToken cancellationToken)
    {
        logger_.LogInformation($"Command {command} handler started");
        var deviceName = dvrOptions_.UniqueId;

        try
        {
            await mqtt
                .ObserveTopic(mqttIntegration_.GetCommandTopic(deviceName, command))
                .TakeUntil(dvrOrMqttDisconnected)
                .SelectMany(async payload =>
                {
                    try
                    {
                        await HandleCommand(dvr, command, payload, stateUpdateRequests);
                    }
                    catch (Exception e)
                    {
                        logger_.LogError(e, $"Error in RunCommandHandler {command}");
                        SetUnhealthy();
                    }
                    return Void.Value;
                })
                .ForEachAsync(_ => { }, cancellationToken);
        }
        finally
        {
            logger_.LogInformation($"Command {command} handler finished");
        }
    }

    private async Task RunButtonHandler(
        IDvrIpWebService dvr,
        IMqttClientService mqtt,
        string button,
        IObservable<Void> dvrOrMqttDisconnected,
        CancellationToken cancellationToken)
    {
        logger_.LogInformation($"Button {button} handler started");
        var deviceName = dvrOptions_.UniqueId;

        try
        {
            await mqtt
                .ObserveTopic(mqttIntegration_.GetButtonTopic(deviceName, button))
                .TakeUntil(dvrOrMqttDisconnected)
                .SelectMany(async payload =>
                {
                    try
                    {
                        await HandleButton(dvr, button, payload);
                    }
                    catch (Exception e)
                    {
                        logger_.LogError(e, $"Error in RunButtonHandler {button}");
                        SetUnhealthy();
                    }
                    return Void.Value;
                })
                .ForEachAsync(_ => { }, cancellationToken);
        }
        finally
        {
            logger_.LogInformation($"{button}: Button handler finished");
        }
    }

    private async Task HandleCommand(
        IDvrIpWebService dvr, 
        string command, 
        string payload,
        IObserver<(Action<DvrState>, Task)> stateUpdateRequests)
    {
        Stopwatch sw = Stopwatch.StartNew();
        Task task = null;

        switch (command)
        {
            case IMqttIntegration.SwitchMailEnable:
                var mailEnable = payload == Defaults.PayloadOn;
                task = dvr.SetMotionDetectConfiguration(mailEnable: mailEnable);
                stateUpdateRequests.OnNext((s => s.MailEnable = payload, task));
                break;

            case IMqttIntegration.SwitchVoiceEnable:
                var voiceEnable = payload == Defaults.PayloadOn;
                task = dvr.SetMotionDetectConfiguration(voiceEnable: voiceEnable);
                stateUpdateRequests.OnNext((s => s.VoiceEnable = payload, task));
                break;

            case IMqttIntegration.SwitchLightEnable:
                var dayNightColorSmart = payload == Defaults.PayloadOn
                    ? (uint)DvrIpWebService.DayNightColorFull
                    : DvrIpWebService.DayNightColorSmart;

                task = dvr.SetCameraParam(dayNightColor: dayNightColorSmart);
                stateUpdateRequests.OnNext((s => s.CameraLight = payload, task));
                break;
        }

        if (task != null)
        {
            await task;
        }

        logger_.LogInformation($"Command {command} with {payload} was executed in {sw.ElapsedMilliseconds}ms");
    }

    private async Task HandleButton(
        IDvrIpWebService dvr, 
        string button, 
        string payload)
    {
        if (payload != Defaults.ButtonPayloadPress)
        {
            return;
        }

        switch (button)
        {
            case IMqttIntegration.CameraRestartButton:
                await dvr.Reboot();
                break;
        }
    }

    private void SetUnhealthy()
    {
        unhealthy_?.OnNext(default);
    }
}