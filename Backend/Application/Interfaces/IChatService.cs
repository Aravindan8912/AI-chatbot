using Application.DTOs;

namespace Application.Interfaces;

public interface IChatService
{
    Task<ChatResponseDto> AskAsync(ChatRequestDto request, CancellationToken cancellationToken = default);
}
