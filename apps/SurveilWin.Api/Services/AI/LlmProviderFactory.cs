namespace SurveilWin.Api.Services.AI;

public class LlmProviderFactory
{
    private readonly IEnumerable<ILlmProvider> _providers;
    private readonly string _defaultProvider;

    public LlmProviderFactory(IEnumerable<ILlmProvider> providers, IConfiguration cfg)
    {
        _providers = providers;
        _defaultProvider = cfg["Ai:Provider"] ?? "ollama";
    }

    public ILlmProvider? GetProvider(string? name = null)
    {
        var target = name ?? _defaultProvider;
        return _providers.FirstOrDefault(p =>
            p.ProviderName.Equals(target, StringComparison.OrdinalIgnoreCase));
    }
}
