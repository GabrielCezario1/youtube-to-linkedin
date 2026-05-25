using Azure.AI.OpenAI;
using Azure.Core;
using YoutubeToLinkedIn.Api.Endpoints;
using YoutubeToLinkedIn.Api.Executors;
using YoutubeToLinkedIn.Api.Hubs;
using YoutubeToLinkedIn.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TranscriptExecutor>();
builder.Services.AddSingleton<PromptLoader>();
builder.Services.AddSingleton<SummaryExecutor>();
builder.Services.AddSingleton<LinkedInWriterExecutor>();
builder.Services.AddSingleton<WorkflowSessionManager>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<WorkflowSessionManager>());

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var endpoint = config["AzureOpenAI:Endpoint"]
        ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured.");
    var apiKey = config["AzureOpenAI:ApiKey"]
        ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is not configured.");
    return new AzureOpenAIClient(new Uri(endpoint), new Azure.AzureKeyCredential(apiKey));
});

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddSignalR();

var app = builder.Build();

app.UseCors();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapHub<WorkflowHub>("/hubs/workflow");
app.MapPost("/api/workflow/start", WorkflowStartEndpoint.Handle);
app.MapPost("/api/workflow/{sessionId}/respond", WorkflowRespondEndpoint.Handle);
app.MapDelete("/api/workflow/{sessionId}", WorkflowCancelEndpoint.Handle);

app.Run();
