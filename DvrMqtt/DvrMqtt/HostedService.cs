using Autofac;
using Dvr;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mqtt;
using Microsoft.Extensions.Logging;

namespace DvrMqtt.DvrMqtt;

public class HostedService : BackgroundService
{
    private readonly IOptions<DvrsOptions> dvrsOptions_;
    private readonly ILifetimeScope lifetimeScope_;
    private readonly ILogger<HostedService> logger_;

    private readonly List<IAsyncDisposable> dvrScopes_ = new();
    private readonly List<Task> tasks_ = new();

    public HostedService(
        IOptions<DvrsOptions> dvrsOptions,
        ILifetimeScope lifetimeScope,
        ILogger<HostedService> logger)
    {
        dvrsOptions_ = dvrsOptions;
        lifetimeScope_ = lifetimeScope;
        logger_ = logger;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await UntilFinished(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var dvrOption in dvrsOptions_.Value.Items)
        {
            var scope = lifetimeScope_
                .BeginLifetimeScope(
                    builder =>
                    {
                        // Add each DVR options in scope.
                        builder.RegisterInstance(new OptionsWrapper<DvrOptions>(dvrOption)).As<IOptions<DvrOptions>>();
                        
                        // Override MQTT registration to share it in scope.
                        builder.RegisterType<MqttClientService>().As<IMqttClientService>().InstancePerLifetimeScope();
                    });

            using (logger_.BeginScope($"[{dvrOption.UniqueId}]"))
            {
                var dvrMqttService = scope.Resolve<IDvrMqttService>();
                var serviceTask = dvrMqttService.Start(stoppingToken);

                dvrScopes_.Add(scope);
                tasks_.Add(serviceTask);
            }
        }

        return Task.CompletedTask;
    }

    private async Task UntilFinished(CancellationToken cancellationToken)
    {
        try
        {
            await Task.WhenAll(tasks_).WaitAsync(cancellationToken);
        }
        finally
        {
            foreach (var scope in dvrScopes_)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await scope.DisposeAsync();
                }
                catch (Exception e)
                {
                    logger_.LogCritical(e, "Error disposing scope");
                }
            }
        }
    }
}