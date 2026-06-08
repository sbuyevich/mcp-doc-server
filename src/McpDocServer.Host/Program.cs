using McpDocServer.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddMcpDocServerCore(builder.Configuration);
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithMcpDocServerTools();

await builder.Build().RunAsync();

public partial class Program;
