using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Assistant.Api;

public sealed class GigaChatClient(HttpClient httpClient, GigaChatOptions options)
{
    private readonly GigaChatOptions _options = options;
    private readonly HttpClient _httpClient = httpClient;

    public async Task<GigaChatResponse> GetChatMessageContentsAsync(
        GigaChatRequest request,
        CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);

        var requestBody = new
        {
            model = "GigaChat-2", // "GigaChat-Pro",// "GigaChat-2",
            messages = request.Messages,
            function_call = "auto",
            functions = new[]
            {
                new
                {
                    name = "SendEmail",
                    description = "Функция для отправки email",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            to = new
                            {
                                type = "string",
                                description = "Кому отправить письмо"
                            },
                            subject = new
                            {
                                type = "string",
                                description = "Заголовок"
                            },
                            body = new
                            {
                                type = "string",
                                description = "Тело"
                            },
                        }
                    },
                    required = new[]
                    {
                        "to",
                        "subject",
                        "body"
                    },
                    few_shot_examples = new[]
                    {
                        // Пример 1 (исходный)
                        new
                        {
                            request =
                                "Сгенерируй дружелюбное письмо и отправь письмо на почту для some_email.com",
                            Params = new
                            {
                                to = "some_email.com",
                                subject = "Дружелюбное письмо",
                                body = "Привет. Я пишу тебе дружелюбное письмо письмо. Просто так."
                            }
                        },

                        // Пример 2 (деловое письмо)
                        new
                        {
                            request =
                                "Отправь официальное письмо клиенту client@company.com о переносе встречи на 15:00",
                            Params = new
                            {
                                to = "client@company.com",
                                subject = "Перенос встречи",
                                body =
                                    "Уважаемый клиент,\n\nСообщаем, что встреча перенесена на 15:00.\n\nС уважением,\nКоманда поддержки"
                            }
                        },

                        // Пример 3 (письмо с напоминанием)
                        new
                        {
                            request = "Напиши письмо с напоминанием о платеже для accounting@firma.ru",
                            Params = new
                            {
                                to = "accounting@firma.ru",
                                subject = "Напоминание о платеже",
                                body =
                                    "Добрый день,\n\nНапоминаем о необходимости оплаты счета №123 до 25.05.2024.\n\nБлагодарим за своевременную оплату!"
                            }
                        },

                        // Пример 4 (короткое уведомление)
                        new
                        {
                            request = "Отправь короткое уведомление на support@service.com о технических работах",
                            Params = new
                            {
                                to = "support@service.com",
                                subject = "Технические работы",
                                body =
                                    "29.05 с 03:00 до 05:00 будут проводиться технические работы. Сервис может быть недоступен."
                            }
                        }
                    }
                }
            },
            //temperature = 1.0,
            //top_p = 0.1,
            stream = false,
            //max_tokens = 100,
            //repetition_penalty = 1.0,
            update_interval = 0
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.CompletionUri);

        httpRequest.Headers.Add(GigaChatHeaderNames.ClientId, "52a6d528-ad36-4f35-b654-b108aa34d85c");
        httpRequest.Headers.Add(GigaChatHeaderNames.RequestId, Guid.NewGuid().ToString());
        httpRequest.Headers.Add(GigaChatHeaderNames.SessionId, Guid.NewGuid().ToString());
        httpRequest.Headers.Add("Authorization", $"Bearer {accessToken}");

        var requestJson = JsonSerializer.Serialize(requestBody);
        httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseObject = JsonSerializer.Deserialize<GigaChatResponse>(responseString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        return responseObject;
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _options.AuthorizationUri);

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", options.ClientSecret);
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
        request.Headers.Add(GigaChatHeaderNames.AuthRequestId, Guid.NewGuid().ToString());

        request.Content = new FormUrlEncodedContent(
            [KeyValuePair.Create<string?, string?>("scope", "GIGACHAT_API_PERS")]);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var authJson = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);

        return authJson.RootElement.GetProperty("access_token").GetString() ?? throw new Exception("Token not found");
    }
}

public static class GigaChatHeaderNames
{
    public const string ClientId = "X-Client-ID";
    public const string RequestId = "X-Request-ID";
    public const string AuthRequestId = "RqUID";
    public const string SessionId = "X-Session-ID";
}

public sealed class GigaChatOptions
{
    public string ClientId { get; set; } = "52a6d528-ad36-4f35-b654-b108aa34d85c";

    public string ClientSecret { get; set; } =
        "NTJhNmQ1MjgtYWQzNi00ZjM1LWI2NTQtYjEwOGFhMzRkODVjOmE3OWYxZmIyLTEwZmUtNGE4MS1hYTE5LWI2ZTRkNmU1OWRmYQ==";

    public string CompletionUri { get; set; } = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

    public string AuthorizationUri { get; set; } = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";
}

public class GigaChatRequest
{
    public required List<Message> Messages { get; set; }
}

public class GigaChatResponse
{
    [JsonPropertyName("choices")] public Choice[] Choices { get; set; }

    public long Created { get; set; }

    public string Model { get; set; }

    public string Object { get; set; }

    public GigaChatUsage Usage { get; set; }
}

public class GigaChatUsage
{
    public int prompt_tokens { get; set; }

    public int completion_tokens { get; set; }

    public int total_tokens { get; set; }

    public int precached_prompt_tokens { get; set; }
}

public class Choice
{
    public int Index { get; set; }

    public string finish_reason { get; set; }

    public Message Message { get; set; }
}

public class Message
{
    public string Role { get; set; }

    public string? Content { get; set; }

    [JsonPropertyName("function_call")] public GigaChatFunctionCall? FunctionCall { get; set; }
}

public class GigaChatFunctionCall
{
    public string Name { get; set; }

    public Dictionary<string, object?> Arguments { get; set; }
}