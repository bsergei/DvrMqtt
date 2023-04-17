namespace Dvr;

public class DvrOptions
{
    public string UniqueId { get; set; } = "";

    public string HostIp { get; set; } = "";

    public int? Port { get; set; }

    public string? User { get; set; }

    public string? Password { get; set; }

    public bool IsNvr { get; set; }
}