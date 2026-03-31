namespace Application.Interfaces;

public interface IOpenAIClient
{
    Task<string> GetCompletionAsync(IReadOnlyList<object> messages, CancellationToken cancellationToken = default);
}
