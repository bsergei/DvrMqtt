using System.Text.Json;
using Autofac.Extensions.DependencyInjection;
using Dvr;
using DvrMqtt.DvrMqtt;
using DvrMqtt.HomeAssistant;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mqtt;

namespace DvrMqtt;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureServices((hostContext, services) =>
            {
                services
                    .Configure<HomeAssistantOptions>(hostContext.Configuration.GetSection("HomeAssistant"))
                    .Configure<MqttServerOptions>(hostContext.Configuration.GetSection("Mqtt"))
                    .Configure<DvrMqttOptions>(hostContext.Configuration.GetSection("DvrMqtt"))
                    .Configure<DvrsOptions>(hostContext.Configuration.GetSection("Cameras"))
                    .AddDvr()
                    .AddHomeAssistant()
                    .AddMqtt()
                    .AddDvrMqtt()
                    .AddSingleton<JsonSerializerOptions>(_ => new JsonSerializerOptions
                    {
                        WriteIndented = true
                    })
                    .AddHostedService<HostedService>();
            });
}