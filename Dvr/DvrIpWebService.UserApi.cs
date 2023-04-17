using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dvr.Commands;

namespace Dvr;

public partial class DvrIpWebService
{
    public const int DayNightColorSmart = 3;
    public const int DayNightColorFull = 4;

    private const string DetectMotionCommand = "Detect.MotionDetect";
    private const string AlarmEventHandler = "EventHandler";
    private const string AlarmVoiceEnable = "VoiceEnable";
    private const string AlarmVoiceType = "VoiceType";
    private const string AlarmMailEnable = "MailEnable";
    
    private const int DefaultAlertVoiceType = 523; // You have entered into monitoring area.
    
    private const string CameraParamCommand = "Camera.Param.[0]";
    private const string ParamDayNightColor = "DayNightColor";

    public async Task SetMotionDetectConfiguration(
        bool? voiceEnable = null, 
        bool? mailEnable = null)
    {
        if (apiSync_ == null)
        {
            throw new ObjectDisposedException(nameof(DvrIpWebService));
        }

        await apiSync_.WaitAsync();

        try
        {
            var info = await GetConfig(DetectMotionCommand);

            var node = JsonNode.Parse(info.ToString())!;

            var nodeArray = node.AsArray();
            foreach (var nodeArrayItem in nodeArray)
            {
                if (voiceEnable != null)
                {
                    nodeArrayItem![AlarmEventHandler]![AlarmVoiceEnable] = voiceEnable.Value;
                    if (voiceEnable.Value)
                    {
                        nodeArrayItem[AlarmEventHandler]![AlarmVoiceType] = DefaultAlertVoiceType;
                    }
                }

                if (mailEnable != null)
                {
                    nodeArrayItem![AlarmEventHandler]![AlarmMailEnable] = mailEnable.Value;
                }
            }

            var json = node.ToJsonString();
            var newInfo = JsonSerializer.Deserialize<JsonElement>(json);

            await SetConfig(DetectMotionCommand, newInfo);
        }
        finally
        {
            apiSync_.Release();
        }
    }

    public async Task<(bool VoiceEnable, bool MailEnable)> GetMotionDetectConfiguration()
    {
        if (apiSync_ == null)
        {
            throw new ObjectDisposedException(nameof(DvrIpWebService));
        }

        await apiSync_.WaitAsync();

        try
        {
            var info = await GetConfig(DetectMotionCommand);

            var node = JsonNode.Parse(info.ToString())!;

            var voiceEnable = true;
            var mailEnable = true;

            var nodeArray = node.AsArray();
            foreach (var nodeArrayItem in nodeArray)
            {
                var oneVoiceEnable = nodeArrayItem![AlarmEventHandler]![AlarmVoiceEnable]!.GetValue<bool>();
                var oneMailEnable = nodeArrayItem[AlarmEventHandler]![AlarmMailEnable]!.GetValue<bool>();

                voiceEnable = voiceEnable && oneVoiceEnable;
                mailEnable = mailEnable && oneMailEnable;
            }

            return (voiceEnable, mailEnable);
        }
        finally
        {
            apiSync_.Release();
        }
    }

    public async Task SetCameraParam(
        uint? dayNightColor = null)
    {
        if (apiSync_ == null)
        {
            throw new ObjectDisposedException(nameof(DvrIpWebService));
        }

        await apiSync_.WaitAsync();

        try
        {
            var info = await GetConfig(CameraParamCommand);

            var node = JsonNode.Parse(info.ToString())!;
            if (dayNightColor != null)
            {
                node[ParamDayNightColor] = $"0x{dayNightColor.Value:X}";
            }

            var json = node.ToJsonString();
            var newInfo = JsonSerializer.Deserialize<JsonElement>(json);

            await SetConfig(CameraParamCommand, newInfo);
        }
        finally
        {
            apiSync_.Release();
        }
    }

    public async Task<(uint DayNightColor, object? _)> GetCameraParam()
    {
        if (apiSync_ == null)
        {
            throw new ObjectDisposedException(nameof(DvrIpWebService));
        }

        await apiSync_.WaitAsync();

        try
        {
            var info = await GetConfig(CameraParamCommand);

            var dayNightColorStr = info.GetProperty(ParamDayNightColor).GetString();
            var dayNightColor = UInt32.Parse(dayNightColorStr?.Substring(2) ?? "", NumberStyles.HexNumber);

            return (dayNightColor, null);
        }
        finally
        {
            apiSync_.Release();
        }
    }

    public async Task Reboot()
    {
        if (apiSync_ == null)
        {
            throw new ObjectDisposedException(nameof(DvrIpWebService));
        }

        await apiSync_.WaitAsync();

        try
        {
            await SystemRequest(new OpMachine { Action = OpMachine.Reboot });
        }
        finally
        {
            apiSync_.Release();
        }
    }
}