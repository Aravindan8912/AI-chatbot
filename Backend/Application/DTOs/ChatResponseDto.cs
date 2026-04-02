namespace Application.DTOs;

public class ChatResponseDto
{
    public string Response { get; set; } = string.Empty;
    public WebsiteDetailsDto? Website { get; set; }
}
