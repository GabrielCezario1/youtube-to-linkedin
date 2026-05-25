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
    private readonly WorkflowSessionManager _sessionManager;
    private readonly string _modelId;

    public LinkedInWriterExecutor(
        PromptLoader promptLoader,
        IHubContext<WorkflowHub> hubContext,
        AzureOpenAIClient openAiClient,
        WorkflowSessionManager sessionManager,
        IConfiguration configuration)
    {
        _promptLoader = promptLoader;
        _hubContext = hubContext;
        _openAiClient = openAiClient;
        _sessionManager = sessionManager;
        _modelId = configuration["AzureOpenAI:ModelId"] ?? "gpt-4o-mini";
    }

    public Task<PostDraftResult> ExecuteAsync(string summary, string postType, string sessionId, string mode = "automatico", CancellationToken cancellationToken = default)
    {
        return mode == "consultado"
            ? ExecuteConsultedAsync(summary, postType, sessionId, cancellationToken)
            : ExecuteAutoAsync(summary, postType, sessionId, cancellationToken);
    }

    // ── Automatic mode (Fase 4 behaviour, unchanged) ─────────────────────────

    private async Task<PostDraftResult> ExecuteAutoAsync(string summary, string postType, string sessionId, CancellationToken cancellationToken)
    {
        await SendWorkflowEvent(sessionId, "in_progress");

        try
        {
            var userMessage = $"Resumo:\n{summary}";
            var result = await GeneratePostAsync(userMessage, postType, sessionId, cancellationToken);
            await SendWorkflowEvent(sessionId, "completed", result: result);
            return result;
        }
        catch (OperationCanceledException)
        {
            await SendWorkflowEvent(sessionId, "error", "Workflow cancelado.", errorCode: "cancelled");
            throw;
        }
        catch (RequestFailedException)
        {
            await SendWorkflowEvent(sessionId, "error", "Ocorreu um erro ao gerar o post. Tente novamente.", errorCode: "llm_error");
            throw;
        }
        catch (Exception)
        {
            await SendWorkflowEvent(sessionId, "error", "Ocorreu um erro ao gerar o post. Tente novamente.", errorCode: "llm_error");
            throw;
        }
    }

    // ── Consulted mode (Fase 5 human-in-the-loop) ────────────────────────────

    private async Task<PostDraftResult> ExecuteConsultedAsync(string summary, string postType, string sessionId, CancellationToken cancellationToken)
    {
        await SendWorkflowEvent(sessionId, "in_progress");

        try
        {
            // Build question list: fixed + up to 3 dynamic
            var questions = new List<string>(GetFixedQuestions(postType));
            var dynamic = await GetDynamicQuestionsAsync(summary, cancellationToken);
            questions.AddRange(dynamic.Take(3));

            // Attach TCS to existing session and emit pause event
            var tcs = new TaskCompletionSource<string[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            _sessionManager.AttachTcs(sessionId, tcs);
            await SendWorkflowEventAwaitingInput(sessionId, questions);

            // Wait for user answers (or timeout/cancellation)
            string[] answers;
            try
            {
                answers = await tcs.Task.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // If the user-initiated CTS was cancelled, emit 'cancelled'.
                // If the sweep expired the session (TCS.TrySetCanceled), it already emitted 'session_expired'.
                if (cancellationToken.IsCancellationRequested)
                    await SendWorkflowEvent(sessionId, "error", "Workflow cancelado.", errorCode: "cancelled");
                throw;
            }

            // Resume: emit in_progress, build enriched prompt, generate post
            await SendWorkflowEvent(sessionId, "in_progress");

            var filteredAnswers = answers
                .Select((a, i) => (answer: a, index: i + 1))
                .Where(x => !string.IsNullOrWhiteSpace(x.answer))
                .Select(x => $"{x.index}. {x.answer.Trim()}")
                .ToList();

            var userMessage = filteredAnswers.Count > 0
                ? $"Resumo:\n{summary}\n\nContexto adicional do autor:\n{string.Join("\n", filteredAnswers)}"
                : $"Resumo:\n{summary}";

            var result = await GeneratePostAsync(userMessage, postType, sessionId, cancellationToken);
            await SendWorkflowEvent(sessionId, "completed", result: result);
            return result;
        }
        catch (OperationCanceledException)
        {
            // Already handled above; re-throw to bubble up to Task.Run catch
            throw;
        }
        catch (RequestFailedException)
        {
            await SendWorkflowEvent(sessionId, "error", "Ocorreu um erro ao gerar o post. Tente novamente.", errorCode: "llm_error");
            throw;
        }
        catch (Exception)
        {
            await SendWorkflowEvent(sessionId, "error", "Ocorreu um erro ao gerar o post. Tente novamente.", errorCode: "llm_error");
            throw;
        }
        finally
        {
            _sessionManager.Cleanup(sessionId);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IEnumerable<string> GetFixedQuestions(string postType) => postType switch
    {
        "storytelling" =>
        [
            "Qual foi o erro ou obstáculo principal?",
            "Qual foi o aprendizado mais valioso?",
            "Para quem é este post?"
        ],
        "lista-pratica" =>
        [
            "Algum item da lista tem contexto da sua experiência?",
            "Para quem é este post?"
        ],
        "opiniao-provocativa" =>
        [
            "Qual é a crença comum que você quer questionar?",
            "Você tem um dado ou exemplo concreto para reforçar?"
        ],
        "noticia" =>
        [
            "Qual é a sua opinião sobre essa novidade? (empolgado, cético, curioso...)",
            "Você já experimentou ou vivenciou algo relacionado a isso?"
        ],
        _ => []
    };

    private async Task<IReadOnlyList<string>> GetDynamicQuestionsAsync(string summary, CancellationToken cancellationToken)
    {
        try
        {
            var chatClient = _openAiClient.GetChatClient(_modelId);
            var systemMsg = "Você é um assistente especialista em conteúdo para LinkedIn.";
            var userMsg = $"""
                Com base no resumo abaixo, gere de 1 a 3 perguntas curtas e específicas que ajudariam um criador de conteúdo a adicionar contexto pessoal ao post. Responda APENAS com um array JSON de strings, sem markdown, sem texto adicional.

                Resumo:
                {summary}
                """;

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemMsg),
                new UserChatMessage(userMsg)
            };

            var options = new ChatCompletionOptions { Temperature = 0.2f, MaxOutputTokenCount = 256 };
            var response = await chatClient.CompleteChatAsync(messages, options, cancellationToken);
            var raw = response.Value.Content[0].Text.Trim();

            if (raw.StartsWith("```"))
            {
                var firstNewline = raw.IndexOf('\n');
                var lastFence = raw.LastIndexOf("```");
                if (firstNewline >= 0 && lastFence > firstNewline)
                    raw = raw[(firstNewline + 1)..lastFence].Trim();
            }

            return JsonSerializer.Deserialize<List<string>>(raw) ?? [];
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Fail silently — flow continues with fixed questions only
            return [];
        }
    }

    private async Task<PostDraftResult> GeneratePostAsync(string userMessage, string postType, string sessionId, CancellationToken cancellationToken)
    {
        var templateContent = _promptLoader.GetPrompt($"templetes/template-{postType}");
        var systemPrompt = $$"""
            {{templateContent}}

            ## Output Format

            Treat the provided summary as the source material for all required context. Generate the post following the template structure above.
            Respond exclusively in valid JSON, without markdown blocks:
            {"draft":"full post text here","templateUsed":"Template Name"}

            The `templateUsed` field must contain exactly one of: "Storytelling", "Lista Prática", "Opinião Provocativa", "Notícia".
            """;
        var chatClient = _openAiClient.GetChatClient(_modelId);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userMessage)
        };

        var options = new ChatCompletionOptions { Temperature = 0.7f, MaxOutputTokenCount = 1200 };

        var response = await chatClient.CompleteChatAsync(messages, options, cancellationToken);
        var rawContent = response.Value.Content[0].Text.Trim();

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

        return result;
    }

    private Task SendWorkflowEvent(
        string sessionId,
        string status,
        string? message = null,
        PostDraftResult? result = null,
        string? errorCode = null)
    {
        object payload;

        if (result is not null)
            payload = new { step = "writing", status, result };
        else if (errorCode is not null && message is not null)
            payload = new { step = "writing", status, errorCode, message };
        else if (message is not null)
            payload = new { step = "writing", status, message };
        else
            payload = new { step = "writing", status };

        return _hubContext.Clients.All.SendAsync("workflowEvent", sessionId, payload);
    }

    private Task SendWorkflowEventAwaitingInput(string sessionId, IReadOnlyList<string> questions)
    {
        var payload = new { step = "writing", status = "awaiting_input", questions };
        return _hubContext.Clients.All.SendAsync("workflowEvent", sessionId, payload);
    }
}
