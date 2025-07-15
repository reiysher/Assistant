using Assistant.Api;
using Assistant.Api.Plugin;
using Assistant.Api.Services.Mailing;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<EmailPlugin>();

builder.Services.AddGigaChat();
builder.Services.AddAgents();

builder.Services.AddKernel();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Эндпоинт для обработки запросов к агенту
app.MapPost("/api/agent", (ChatCompletionAgent agent, AgentRequest request, CancellationToken cancellationToken) =>
{
    return Results.Ok(CompleteSteamingAsync(request, agent, cancellationToken));
});

app.Run();

async IAsyncEnumerable<AgentResponseItem<ChatMessageContent>> CompleteSteamingAsync(
    AgentRequest agentRequest,
    ChatCompletionAgent chatCompletionAgent,
    CancellationToken cancellationToken1)
{
    var chatHistory = new ChatHistory();
    // chatHistory.AddUserMessage(request.UserQuery);
    var thread = new ChatHistoryAgentThread(chatHistory);

    ChatMessageContent message = new ChatMessageContent(AuthorRole.User, agentRequest.UserQuery);

    var asyncEnumerable = chatCompletionAgent.InvokeAsync(
        message,
        thread,
        cancellationToken: cancellationToken1);

    await foreach (var item in asyncEnumerable.ConfigureAwait(false))
    {
        yield return item;
    }
}