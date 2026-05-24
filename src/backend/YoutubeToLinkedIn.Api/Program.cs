using YoutubeToLinkedIn.Api.Endpoints;
using YoutubeToLinkedIn.Api.Executors;
using YoutubeToLinkedIn.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<TranscriptExecutor>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddSignalR();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();

app.MapHub<WorkflowHub>("/hubs/workflow");
app.MapPost("/api/workflow/start", WorkflowStartEndpoint.Handle);

app.Run();
