namespace DvrMqtt.DvrMqtt;

public class DvrMqttOptions
{
    public string ClientId { get; set; } = "";

    public string TopicBase { get; set; } = "";

    /// <summary>
    /// Update interval in seconds. 0 to disable.
    /// </summary>
    public int SnapshotsUpdateInterval { get; set; }
}