using Microsoft.Extensions.DependencyInjection;

namespace Dvr;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddDvr(this IServiceCollection services)
    {
        return services
            .AddHttpClient()
            .AddTransient<IDvrHttpWebService, DvrHttpWebService>()
            .AddTransient<IDvrIpWebService, DvrIpWebService>()
            .AddSingleton<IDvrIpPacket, DvrIpPacket>()
            ;
    }
}