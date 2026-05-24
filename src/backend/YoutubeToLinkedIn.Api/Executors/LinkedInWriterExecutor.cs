using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.SignalR;
using OpenAI.Chat;
using YoutubeToLinkedIn.Api.Hubs;
using YoutubeToLinkedIn.Api.Models;
using YoutubeToLinkedIn.Api.Services;

namespace YoutubeToLinkedIn.Api.Executors;

public class LinkedInWriterExecutor
{
    private readonly PromptLoader _promptLoader;
    private readonly IHubContext<WorkflowHub> _hubContext;
    private readonly AzureOpenAIClient _openAiClient;
    private readonly string _modelId;

    public LinkedInWriterExecutor(
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

    public async Task<PostDraftResult> ExecuteAsync(string summary, string postType, string sessionId)
    {
        await SendWorkflowEvent(sessionId, "in_progress");

        try
        {
            var systemPrompt = _promptLoader.GetPrompt("linkedin-writer-system");

            var chatClient = _openAiClient.GetChatClient(_modelId);

            var userMessage = $"Tipo de post: {postType}\n\nResumo:\n{summary}";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userMessage)
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.7f,
                MaxOutputTokenCount = 1200
            };

            var response = await chatClient.CompleteChatAsync(messages, options);
            var rawContent = response.Value.Content[0].Text.Trim();

            // Strip optional ```json fences from the model response
            if (rawContent.StartsWith("```"))
            {
                var firstNewline = rawContent.IndexOf('\n');
                var lastFence = rawContent.LastIndexOf("```");
                if (firstNewline >= 0 && lastFence > firstNewline)
                    rawContent = rawContent[(firstNewline + 1)..lastFence].Trim();
            }

            var result = JsonSerializer.Deserialize<PostDraftResult>(
                rawContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result is null || string.IsNullOrWhiteSpace(result.Draft))
                throw new InvalidOperationException("A resposta do modelo não contém um rascunho válido.");

            await SendWorkflowEvent(sessionId, "completed", result: result);
            return result;
        }
        catch (RequestFailedException)
        {
            await SendWorkflowEvent(sessionId, "error",
                "Ocorreu um erro ao gerar o post. Tente novamente.");
            throw;
        }
        catch (TaskCanceledException)
        {
            await SendWorkflowEvent(sessionId, "error",
                "Ocorreu um erro ao gerar o post. Tente novamente.");
            throw;
        }
        catch (Exception)
        {
            await SendWorkflowEvent(sessionId, "error",
                "Ocorreu um erro ao gerar o post. Tente novamente.");
            throw;
        }
    }

    private Task SendWorkflowEvent(
        string sessionId,
        string status,
        string? message = null,
        PostDraftResult? result = null)
    {
        object payload;

        if (result is not null)
            payload = new { step = "writing", status, result };
        else if (message is not null)
            payload = new { step = "writing", status, message };
        else
            payload = new { step = "writing", status };

        return _hubContext.Clients.All.SendAsync("workflowEvent", sessionId, payload);
    }
}
