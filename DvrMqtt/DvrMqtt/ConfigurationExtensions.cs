using Microsoft.Extensions.DependencyInjection;

namespace DvrMqtt.DvrMqtt;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddDvrMqtt(this IServiceCollection services)
    {
        return services
            .AddSingleton<IMqttIntegration, MqttIntegration>()
            .AddTransient<IDvrMqttEvents, DvrMqttEvents>()
            .AddTransient<IDvrMqttService, DvrMqttService>()
            .AddTransient<IDvrMqttStatePublisher, DvrMqttStatePublisher>()
            .AddTransient<IDvrMqttCommandHandler, DvrMqttCommandHandler>()
            ;
    }
}