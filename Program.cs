using Microsoft.Extensions.AI;
using FormVisualizer.Components;
using FormVisualizer.Services;
using FormVisualizer.Services.Ingestion;
using OpenAI;
using System.ClientModel;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var apiKey = builder.Configuration["ApiToken"] ?? throw new Exception("No API token found!");

builder.Services.AddSingleton<KernelPluginCollection>((serviceProvider) => 
    [
        // KernelPluginFactory.CreateFromObject(serviceProvider.GetRequiredService<LightsPlugin>()),
        // KernelPluginFactory.CreateFromObject(serviceProvider.GetRequiredService<SpeakerPlugin>())
    ]
);

builder.Services.AddOpenAIChatCompletion(
    modelId: "gpt-4o-mini",
    apiKey: apiKey
    // orgId: "YOUR_ORG_ID", // Optional; for OpenAI deployment
    // serviceId: "YOUR_SERVICE_ID" // Optional; for targeting specific services within Semantic Kernel
);

builder.Services.AddTransient((serviceProvider)=> {
    KernelPluginCollection pluginCollection = serviceProvider.GetRequiredService<KernelPluginCollection>();

    return new Kernel(serviceProvider, pluginCollection);
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
