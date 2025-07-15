using Microsoft.SemanticKernel.ChatCompletion;

namespace Assistant.Api;

public static class GigaChatExtensions
{
    public static IServiceCollection AddGigaChat(
        this IServiceCollection services)
    {
        services.AddSingleton<GigaChatOptions>();

        services.AddTransient<GigaChatClient>();

        services.AddHttpClient<GigaChatClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<IChatCompletionService, GigaChatCompletionService>();

        return services;
    }
}