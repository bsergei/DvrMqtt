using System.Text.Json;
using Dvr.Commands;

namespace Dvr;

public interface IDvrIpWebService
{
    Task Run(CancellationToken cancellationToken);

    Task WhenConnected();

    Task SystemRequest(OpMachine opMachine);

    Task<JsonElement> GetConfig(string name);

    Task SetConfig(string name, JsonElement data);

    IObservable<AlarmInfo> ObserveAlarms();

    Task SetMotionDetectConfiguration(
        bool? voiceEnable = null, 
        bool? mailEnable = null);

    Task<(bool VoiceEnable, bool MailEnable)> GetMotionDetectConfiguration();

    Task SetCameraParam(uint? dayNightColor = null);

    Task<(uint DayNightColor, object? _)> GetCameraParam();

    Task Reboot();
}