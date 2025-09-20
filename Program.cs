using Microsoft.Extensions.AI;
using FormVisualizer.Components;
using FormVisualizer.Services;
using FormVisualizer.Services.Ingestion;
using OpenAI;
using System.ClientModel;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Protocol;
using ModelContextProtocol;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var apiKey = builder.Configuration["ApiToken"] ?? throw new Exception("No API token found!");

builder.Services.AddOpenAIChatCompletion(
    modelId: "gpt-4o-mini",
    apiKey: apiKey
// orgId: "YOUR_ORG_ID", // Optional; for OpenAI deployment
// serviceId: "YOUR_SERVICE_ID" // Optional; for targeting specific services within Semantic Kernel
);

var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "AppMaker",
    Command = @"C:\Users\andre\.dotnet\tools\Greg.Xrm.Mcp.AppMaker.exe",
    Arguments = ["--dataverseUrl","https://org41df0750.crm4.dynamics.com"],
});

var client = await McpClientFactory.CreateAsync(clientTransport);
var tools = await client.ListToolsAsync();

builder.Services.AddTransient((serviceProvider) =>
{
    var kernel = new Kernel(serviceProvider);
    kernel.Plugins.AddFromFunctions("Tools", tools.Select(aiFunction => aiFunction.AsKernelFunction()));

    return kernel;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static Task<IMcpClient> CreateMcpClientAsync(string command, string name,
        Kernel? kernel = null,
        Func<Kernel, CreateMessageRequestParams?, IProgress<ProgressNotificationValue>, CancellationToken, Task<CreateMessageResult>>? samplingRequestHandler = null,
        ILoggerFactory? loggerFactory = null)
    {
        KernelFunction? skSamplingHandler = null;
    

        // Create and return the MCP client
    return McpClientFactory.CreateAsync(
        clientTransport: new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = name,
            Command = command, // Path to the MCPServer executable
        }),
        clientOptions: samplingRequestHandler != null ? new McpClientOptions()
        {
            Capabilities = new ClientCapabilities
            {
                Sampling = new SamplingCapability
                {
                    SamplingHandler = InvokeHandlerAsync
                },
            },
        } : null,
        loggerFactory: loggerFactory
     );

        async ValueTask<CreateMessageResult> InvokeHandlerAsync(CreateMessageRequestParams? request, IProgress<ProgressNotificationValue> progress, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            skSamplingHandler ??= KernelFunctionFactory.CreateFromMethod(
                (CreateMessageRequestParams? request, IProgress<ProgressNotificationValue> progress, CancellationToken ct) =>
                {
                    return samplingRequestHandler(kernel!, request, progress, ct);
                },
                "MCPSamplingHandler"
            );

            // The argument names must match the parameter names of the delegate the SK Function is created from
            KernelArguments kernelArguments = new()
            {
                ["request"] = request,
                ["progress"] = progress
            };

            FunctionResult functionResult = await skSamplingHandler.InvokeAsync(kernel!, kernelArguments, cancellationToken);

            return functionResult.GetValue<CreateMessageResult>()!;
        }
    }
