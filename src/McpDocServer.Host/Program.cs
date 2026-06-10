using McpDocServer.Host;
using McpDocServer.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

var bootstrapConfiguration = new ConfigurationManager();
bootstrapConfiguration.SetBasePath(AppContext.BaseDirectory);
bootstrapConfiguration.AddJsonFile("appsettings.json", optional: true);
bootstrapConfiguration.AddEnvironmentVariables();
bootstrapConfiguration.AddCommandLine(args);

var transport = bootstrapConfiguration[
    $"{McpDocServerOptions.SectionName}:Transport"] ?? "stdio";

if (transport.Equals("http", StringComparison.Ordinal))
{
    await RunHttpAsync(args);
}
else
{
    await RunStdioAsync(args);
}

static async Task RunStdioAsync(string[] args)
{
    var builder = Host.CreateApplicationBuilder(
        new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });

    ConfigureLogging(builder.Logging);
    builder.Services.AddMcpDocServerCore(builder.Configuration);
    builder.Services.AddMcpServer()
        .WithStdioServerTransport()
        .WithMcpDocServerTools();

    await builder.Build().RunAsync();
}

static async Task RunHttpAsync(string[] args)
{
    var builder = WebApplication.CreateBuilder(
        new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });

    ConfigureLogging(builder.Logging);
    builder.Services.AddMcpDocServerCore(builder.Configuration);
    builder.Services.AddMcpServer()
        .WithHttpTransport(options => options.Stateless = true)
        .WithMcpDocServerTools();

    var app = builder.Build();
    var options = app.Services
        .GetRequiredService<IOptions<McpDocServerOptions>>()
        .Value;
    app.MapMcp(options.Http.Path);
    await app.RunAsync(options.Http.Url);
}

static void ConfigureLogging(ILoggingBuilder logging)
{
    logging.ClearProviders();
    logging.AddConsole(options =>
    {
        options.LogToStandardErrorThreshold = LogLevel.Trace;
    });
}

public partial class Program;
