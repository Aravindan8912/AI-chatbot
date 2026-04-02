using System.Text;
using System.Text.RegularExpressions;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class ChatService : IChatService
{
    private const string GreetingResponse =
        "Hey buddy! How can I help make your day better?";

    private static readonly HashSet<string> CasualGreetings = new(StringComparer.OrdinalIgnoreCase)
    {
        "hi",
        "hai",
        "hello",
        "hey",
        "hey buddy",
        "hi buddy",
        "hello buddy",
        "hey there",
        "hi there",
        "hello there",
        "good morning",
        "good afternoon",
        "good evening",
        "yo",
        "sup",
        "howdy",
        "hiya",
        "greetings"
    };

    private static readonly HashSet<string> GreetingFillers = new(StringComparer.OrdinalIgnoreCase)
    {
        "there",
        "buddy",
        "mate",
        "friend",
        "my",
        "pal",
        "everyone",
        "all"
    };

    private static readonly Regex ElongatedGreetingToken = new(
        @"^(hi+|ha+i+|hey+|he+y+|hell+o+|h+i+|yo+|y+o+|sup|wassup)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex UrlRegex = new(
        @"https?://[^\s]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IOpenAIClient _openAIClient;
    private readonly IWebsiteAnalyzer _websiteAnalyzer;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IOpenAIClient openAIClient,
        IWebsiteAnalyzer websiteAnalyzer,
        ILogger<ChatService> logger)
    {
        _openAIClient = openAIClient;
        _websiteAnalyzer = websiteAnalyzer;
        _logger = logger;
    }

    public async Task<ChatResponseDto> AskAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        var userMessage = request.Message.Trim();
        _logger.LogInformation("Chat request received. MessageLength={MessageLength}", userMessage.Length);

        if (IsCasualGreeting(userMessage))
        {
            _logger.LogInformation("Message classified as casual greeting");
            return new ChatResponseDto { Response = GreetingResponse };
        }

        if (TryExtractWebsiteUrl(userMessage, out var websiteUrl) && IsWebsiteAnalysisRequest(userMessage))
        {
            var details = await _websiteAnalyzer.GetWebsiteDetailsAsync(websiteUrl, cancellationToken);
            if (details is not null)
            {
                _logger.LogInformation("Returning website analysis for {Host}", details.Host);
                return new ChatResponseDto
                {
                    Response = BuildWebsiteDetailsResponse(details),
                    Website = details
                };
            }
        }

        var messages = new List<object>
        {
            new Dictionary<string, string>
            {
                ["role"] = "system",
                ["content"] = "You are a cheerful, helpful personal assistant. Keep greetings warm and concise, and offer clear help for the user's next step."
            },
            new Dictionary<string, string>
            {
                ["role"] = "user",
                ["content"] = userMessage
            }
        };

        var text = await _openAIClient.GetCompletionAsync(messages, cancellationToken);
        _logger.LogInformation("OpenAI response received. Empty={IsEmpty}", string.IsNullOrWhiteSpace(text));
        return new ChatResponseDto
        {
            Response = string.IsNullOrWhiteSpace(text)
                ? "I'm here and ready to help. Tell me what you need, and we'll work through it together."
                : text.Trim()
        };
    }

    private static bool IsCasualGreeting(string message)
    {
        var normalized = NormalizeMessage(message);
        if (string.IsNullOrEmpty(normalized))
            return false;

        if (CasualGreetings.Contains(normalized))
            return true;

        if (IsGoodDaytimeGreetingWithOptionalFillers(normalized))
            return true;

        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return false;

        var hasCoreGreeting = false;
        foreach (var part in parts)
        {
            if (CasualGreetings.Contains(part) || ElongatedGreetingToken.IsMatch(part))
            {
                hasCoreGreeting = true;
                continue;
            }

            if (GreetingFillers.Contains(part))
                continue;

            return false;
        }

        return hasCoreGreeting;
    }

    private static bool IsGoodDaytimeGreetingWithOptionalFillers(string normalized)
    {
        foreach (var day in new[] { "good morning", "good afternoon", "good evening" })
        {
            if (!normalized.StartsWith(day, StringComparison.OrdinalIgnoreCase))
                continue;

            var rest = normalized[day.Length..].Trim();
            if (rest.Length == 0)
                return true;

            foreach (var part in rest.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!GreetingFillers.Contains(part))
                    return false;
            }

            return true;
        }

        return false;
    }

    private static string NormalizeMessage(string message)
    {
        var builder = new StringBuilder(message.Length);
        var previousWasSpace = false;

        foreach (var ch in message.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                previousWasSpace = false;
                continue;
            }

            if (!char.IsWhiteSpace(ch) || previousWasSpace)
                continue;

            builder.Append(' ');
            previousWasSpace = true;
        }

        return builder.ToString().Trim();
    }

    private static bool TryExtractWebsiteUrl(string message, out Uri uri)
    {
        var match = UrlRegex.Match(message);
        if (!match.Success)
        {
            uri = null!;
            return false;
        }

        var raw = match.Value.TrimEnd('.', ',', ';', ':', '!', '?', ')', ']', '}', '"', '\'');
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var parsedUri))
        {
            uri = null!;
            return false;
        }

        if (parsedUri.Scheme != Uri.UriSchemeHttp && parsedUri.Scheme != Uri.UriSchemeHttps)
        {
            uri = null!;
            return false;
        }

        uri = parsedUri;
        return true;
    }

    private static bool IsWebsiteAnalysisRequest(string message)
    {
        var normalized = NormalizeMessage(message);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        return normalized.Contains("analyze")
               || normalized.Contains("analysis")
               || normalized.Contains("website details")
               || normalized.Contains("site details")
               || normalized.Contains("link details")
               || normalized.Contains("website")
               || normalized.Contains("link");
    }

    private static string BuildWebsiteDetailsResponse(WebsiteDetailsDto details)
    {
        var lines = new List<string>
        {
            $"Website: {details.Url}",
            $"Host: {details.Host}"
        };

        if (!string.IsNullOrWhiteSpace(details.FinalUrl))
            lines.Add($"Final URL: {details.FinalUrl}");
        if (details.StatusCode is not null)
            lines.Add($"HTTP Status: {details.StatusCode}");
        if (!string.IsNullOrWhiteSpace(details.Title))
            lines.Add($"Title: {details.Title}");
        if (!string.IsNullOrWhiteSpace(details.Description))
            lines.Add($"Description: {details.Description}");

        return string.Join(Environment.NewLine, lines);
    }
}
