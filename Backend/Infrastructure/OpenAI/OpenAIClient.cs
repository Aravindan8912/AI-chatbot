using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.OpenAI;

public class OpenAIClient : IOpenAIClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public OpenAIClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<string> GetCompletionAsync(IReadOnlyList<object> messages, CancellationToken cancellationToken = default)
    {
        var apiKey = _config["OpenAI:ApiKey"]
                     ?? _config["OPENAI_API_KEY"]
                     ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is not configured. Set OpenAI:ApiKey or OPENAI_API_KEY.");

        var model = _config["OpenAI:Model"] ?? "gpt-4o-mini";

        var payload = new { model, messages };
        using var content = new StringContent(
            JsonSerializer.Serialize(payload, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI API error ({(int)response.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            throw new InvalidOperationException("OpenAI returned no choices.");

        var message = choices[0].GetProperty("message");
        return ExtractContent(message).Trim();
    }

    private static string ExtractContent(JsonElement message)
    {
        if (!message.TryGetProperty("content", out var content))
            return string.Empty;

        if (content.ValueKind == JsonValueKind.String)
            return content.GetString() ?? string.Empty;

        if (content.ValueKind != JsonValueKind.Array)
            return string.Empty;

        var builder = new StringBuilder();

        foreach (var item in content.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            if (!item.TryGetProperty("type", out var type) ||
                !string.Equals(type.GetString(), "text", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!item.TryGetProperty("text", out var text) || text.ValueKind != JsonValueKind.String)
                continue;

            builder.Append(text.GetString());
        }

        return builder.ToString();
    }
}
