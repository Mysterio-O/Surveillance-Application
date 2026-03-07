namespace SurveilWin.Api.Services.AI;

public interface ILlmProvider
{
    string ProviderName { get; }
    Task<string?> CompleteAsync(string prompt, CancellationToken ct = default);
}
