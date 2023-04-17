using Microsoft.Extensions.DependencyInjection;

namespace Mqtt;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddMqtt(this IServiceCollection services)
    {
        return services
            .AddSingleton<IMqttClientService, MqttClientService>();
    }
}