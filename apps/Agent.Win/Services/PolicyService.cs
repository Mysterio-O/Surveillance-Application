using Surveil.Contracts;
using Surveil.Utils;

namespace Surveil.Agent.Services;

/// <summary>
/// Enforces app allow / deny list rules from <see cref="AppConfig"/>.
/// </summary>
public class PolicyService
{
    private readonly AppConfig _cfg;

    public PolicyService(AppConfig cfg) => _cfg = cfg;

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns <c>true</c> when a frame for <paramref name="processName"/>
    /// should be captured and stored.
    /// </summary>
    public bool ShouldCapture(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return true;

        string name = processName.ToLowerInvariant();

        // Explicit deny takes priority over everything else.
        if (_cfg.DeniedApps.Any(d => name.Contains(d.ToLowerInvariant())))
        {
            Log.Info($"[Policy] Skip – denied app: {processName}");
            return false;
        }

        // If an allow-list is configured, only capture listed apps.
        if (_cfg.AllowedApps.Count > 0)
        {
            bool allowed = _cfg.AllowedApps.Any(a => name.Contains(a.ToLowerInvariant()));
            if (!allowed)
                Log.Info($"[Policy] Skip – not in allow-list: {processName}");
            return allowed;
        }

        return true;
    }

    // Convenience pass-throughs so callers don't need to reach into AppConfig.
    public bool SaveThumbnails => _cfg.SaveThumbnails;
    public bool FullTraceMode  => _cfg.FullTraceMode;
    public bool EnableOcr      => _cfg.EnableOcr;
    public bool EnableEmbeddings => _cfg.EnableEmbeddings;
}
