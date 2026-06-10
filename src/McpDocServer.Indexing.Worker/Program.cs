using McpDocServer.Indexing.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var runOnce = args.Any(argument =>
    argument.Equals("--once", StringComparison.OrdinalIgnoreCase));
var hostArguments = args
    .Where(argument => !argument.Equals("--once", StringComparison.OrdinalIgnoreCase))
    .ToArray();

var builder = Host.CreateApplicationBuilder(
    new HostApplicationBuilderSettings
    {
        Args = hostArguments,
        ContentRootPath = AppContext.BaseDirectory
    });

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddIndexingWorkerCore(builder.Configuration);

if (!runOnce)
{
    builder.Services.AddHostedService<IndexingBackgroundService>();
}

using var host = builder.Build();

if (!runOnce)
{
    await host.RunAsync();
    return 0;
}

try
{
    await host.StartAsync();
    var succeeded = await host.Services
        .GetRequiredService<IndexingRunExecutor>()
        .RunOnceAsync(CancellationToken.None);
    await host.StopAsync();
    return succeeded ? 0 : 1;
}
catch (Exception exception)
{
    host.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("McpDocServer.Indexing.Worker")
        .LogError(exception, "One-shot indexing failed.");
    return 1;
}

public partial class Program;
