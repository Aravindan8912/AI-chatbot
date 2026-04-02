namespace Application.DTOs;

public class WebsiteDetailsDto
{
    public string Url { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string? FinalUrl { get; set; }
    public int? StatusCode { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
}
