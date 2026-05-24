using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using YoutubeToLinkedIn.Api.Hubs;
using YoutubeToLinkedIn.Api.Services;

namespace YoutubeToLinkedIn.Api.Executors;

public class SummaryExecutor
{
    private readonly PromptLoader _promptLoader;
    private readonly IHubContext<WorkflowHub> _hubContext;
    private readonly AzureOpenAIClient _openAiClient;
    private readonly string _modelId;

    public SummaryExecutor(
        PromptLoader promptLoader,
        IHubContext<WorkflowHub> hubContext,
        AzureOpenAIClient openAiClient,
        IConfiguration configuration)
    {
        _promptLoader = promptLoader;
        _hubContext = hubContext;
        _openAiClient = openAiClient;
        _modelId = configuration["AzureOpenAI:ModelId"] ?? "gpt-4o-mini";
    }

    public async Task<string> ExecuteAsync(string transcript, string sessionId)
    {
        await SendWorkflowEvent(sessionId, "in_progress");

        try
        {
            var systemPrompt = _promptLoader.GetPrompt("summarizer-system");

            var chatClient = _openAiClient.GetChatClient(_modelId);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(transcript)
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.3f,
                MaxOutputTokenCount = 1200
            };

            var response = await chatClient.CompleteChatAsync(messages, options);
            var content = response.Value.Content[0].Text;

            await SendWorkflowEvent(sessionId, "completed");
            return content;
        }
        catch (RequestFailedException)
        {
            await SendWorkflowEvent(sessionId, "error",
                "Ocorreu um erro ao processar o conteúdo. Tente novamente.");
            throw;
        }
        catch (TaskCanceledException)
        {
            await SendWorkflowEvent(sessionId, "error",
                "Ocorreu um erro ao processar o conteúdo. Tente novamente.");
            throw;
        }
        catch (Exception)
        {
            await SendWorkflowEvent(sessionId, "error",
                "Ocorreu um erro ao processar o conteúdo. Tente novamente.");
            throw;
        }
    }

    private Task SendWorkflowEvent(string sessionId, string status, string? message = null)
    {
        object payload = message is null
            ? new { step = "summary", status }
            : new { step = "summary", status, message };

        return _hubContext.Clients.All.SendAsync("workflowEvent", sessionId, payload);
    }
}
