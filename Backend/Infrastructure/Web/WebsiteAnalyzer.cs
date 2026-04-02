using System.Net;
using System.Text.RegularExpressions;
using Application.DTOs;
using Application.Interfaces;

namespace Infrastructure.Web;

public class WebsiteAnalyzer : IWebsiteAnalyzer
{
    private static readonly Regex TitleRegex = new(
        @"<title[^>]*>(?<value>.*?)</title>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex DescriptionRegex = new(
        @"<meta[^>]+name\s*=\s*[""']description[""'][^>]*content\s*=\s*[""'](?<value>[^""']*)[""'][^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex OgDescriptionRegex = new(
        @"<meta[^>]+property\s*=\s*[""']og:description[""'][^>]*content\s*=\s*[""'](?<value>[^""']*)[""'][^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private readonly HttpClient _httpClient;

    public WebsiteAnalyzer(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WebsiteDetailsDto?> GetWebsiteDetailsAsync(Uri websiteUrl, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, websiteUrl);
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (compatible; ChatAnalyzer/1.0)");

        try
        {
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            return new WebsiteDetailsDto
            {
                Url = websiteUrl.ToString(),
                Host = websiteUrl.Host,
                FinalUrl = response.RequestMessage?.RequestUri?.ToString(),
                StatusCode = (int)response.StatusCode,
                Title = ExtractValue(TitleRegex, html),
                Description = ExtractMetaDescription(html)
            };
        }
        catch
        {
            return new WebsiteDetailsDto
            {
                Url = websiteUrl.ToString(),
                Host = websiteUrl.Host,
                FinalUrl = websiteUrl.ToString(),
                StatusCode = null,
                Title = null,
                Description = "Unable to fetch website details."
            };
        }
    }

    private static string? ExtractMetaDescription(string html)
    {
        var description = ExtractValue(DescriptionRegex, html);
        if (!string.IsNullOrWhiteSpace(description))
            return description;

        return ExtractValue(OgDescriptionRegex, html);
    }

    private static string? ExtractValue(Regex regex, string html)
    {
        var match = regex.Match(html);
        if (!match.Success)
            return null;

        var value = match.Groups["value"].Value;
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return WebUtility.HtmlDecode(value).Trim();
    }
}
