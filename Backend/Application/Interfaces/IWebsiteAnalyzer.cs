using Application.DTOs;

namespace Application.Interfaces;

public interface IWebsiteAnalyzer
{
    Task<WebsiteDetailsDto?> GetWebsiteDetailsAsync(Uri websiteUrl, CancellationToken cancellationToken = default);
}
