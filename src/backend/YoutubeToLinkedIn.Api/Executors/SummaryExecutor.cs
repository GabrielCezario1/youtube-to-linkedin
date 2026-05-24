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

    public async Task<string> ExecuteAsync(string transcript, string sessionId, CancellationToken cancellationToken = default)
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

            var response = await chatClient.CompleteChatAsync(messages, options, cancellationToken);
            var content = response.Value.Content[0].Text;

            await SendWorkflowEvent(sessionId, "completed");
            return content;
        }
        catch (OperationCanceledException)
        {
            await SendWorkflowEvent(sessionId, "error", "Workflow cancelado.", "cancelled");
            throw;
        }
        catch (RequestFailedException)
        {
            await SendWorkflowEvent(sessionId, "error",
                "Ocorreu um erro ao processar o conteúdo. Tente novamente.", "llm_error");
            throw;
        }
        catch (Exception)
        {
            await SendWorkflowEvent(sessionId, "error",
                "Ocorreu um erro ao processar o conteúdo. Tente novamente.", "llm_error");
            throw;
        }
    }

    private Task SendWorkflowEvent(string sessionId, string status, string? message = null, string? errorCode = null)
    {
        object payload;
        if (errorCode is not null && message is not null)
            payload = new { step = "summary", status, errorCode, message };
        else if (message is not null)
            payload = new { step = "summary", status, message };
        else
            payload = new { step = "summary", status };

        return _hubContext.Clients.All.SendAsync("workflowEvent", sessionId, payload);
    }
}
