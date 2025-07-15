using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.TextGeneration;

namespace Assistant.Api;

public sealed class GigaChatCompletionService(
    GigaChatClient client,
    ILogger<GigaChatCompletionService> logger)
    : IChatCompletionService
{
    private readonly GigaChatClient _client = client;
    private readonly ILogger<GigaChatCompletionService> _logger = logger;

    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var request = new GigaChatRequest
        {
            Messages = chatHistory
                .Select(m => new Message
                {
                    Role = m.Role.ToString().ToLower(),
                    Content = m.Content

                })
                .ToList(),
        };

        // 1. Прокинули executionSettings параметры FunctionChoiceBehavior (auto)
        // 2. Из kernel собрали метаданные функций
        // 3. Примеры вызова функции возможно самому придется

        var result = await _client.GetChatMessageContentsAsync(request, cancellationToken);

        if (result.Choices.FirstOrDefault()?.Message?.FunctionCall is not null)
        {
            // FunctionsProcessor - делегировать ему
            return [await HandleFunctionCallAsync(result, kernel!, cancellationToken)];
        }

        var message = new ChatMessageContent(AuthorRole.Assistant,
            result?.Choices[0]?.Message?.Content ?? "Ошибка");

        return new List<ChatMessageContent> { message };
    }

    public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    private async Task<ChatMessageContent> HandleFunctionCallAsync(
        GigaChatResponse llmResponse,
        Kernel kernel,
        CancellationToken cancellationToken)
    {
        var functionCall = llmResponse.Choices.First().Message.FunctionCall;

        // 1. Извлекаем данные о функции
        KernelFunction function = FindFunction(kernel, functionCall.Name);
        var arguments = functionCall.Arguments;

        // 2. Вызываем функцию через Kernel
        var result = await kernel.InvokeAsync(function, new KernelArguments(arguments), cancellationToken: cancellationToken);

        // 3. Возвращаем результат выполнения
        return new ChatMessageContent(
            AuthorRole.Tool,
            result.GetValue<string>() ?? "Функция выполнена",
            metadata: new Dictionary<string, object?>
            {
                ["function"] = functionCall.Name,
                ["arguments"] = arguments
            });
    }

    private KernelFunction? FindFunction(Kernel kernel, string functionName)
    {
        foreach (var plugin in kernel.Plugins)
        {
            if (plugin.TryGetFunction(functionName, out var function))
            {
                return function;
            }
        }
        return null;
    }
}