namespace Mqtt;

public class MqttServerOptions
{
    public string ClientId { get; set; } = String.Empty;

    public string ConnectionUri { get; set; } = String.Empty;

    public string Username { get; set; } = String.Empty;
        
    public string Password { get; set; } = String.Empty;
}