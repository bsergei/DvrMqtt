using Microsoft.Extensions.DependencyInjection;

namespace DvrMqtt.HomeAssistant;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddHomeAssistant(this IServiceCollection services)
    {
        return services
            .AddTransient<IHomeAssistant, HomeAssistant>()
            .AddTransient<IHomeAssistantIntegration, HomeAssistantIntegration>();
    }
}