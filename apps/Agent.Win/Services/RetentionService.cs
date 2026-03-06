using Surveil.Contracts;
using Surveil.Utils;

namespace Surveil.Agent.Services;

/// <summary>
/// Deletes thumbnails and summary files that exceed their configured retention windows.
/// </summary>
public class RetentionService
{
    private readonly AppConfig _cfg;

    public RetentionService(AppConfig cfg) => _cfg = cfg;

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Runs thumbnail and summary cleanup. Safe to call at startup or
    /// periodically; any individual file failure is logged and skipped.
    /// </summary>
    public void Cleanup()
    {
        if (_cfg.ThumbnailRetentionDays > 0)
            RunCleanup("thumb_*.jpg",   _cfg.ThumbnailRetentionDays, "thumbnail");

        if (_cfg.SummaryRetentionDays > 0)
            RunCleanup("summary_*.json", _cfg.SummaryRetentionDays, "summary");
    }

    // -----------------------------------------------------------------------
    // Implementation
    // -----------------------------------------------------------------------

    private void RunCleanup(string pattern, int retentionDays, string label)
    {
        var dir = _cfg.SessionsDir;
        if (!Directory.Exists(dir)) return;

        var cutoff  = DateTime.UtcNow.AddDays(-retentionDays);
        int deleted = 0;

        foreach (var file in Directory.GetFiles(dir, pattern))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(file) < cutoff)
                {
                    File.Delete(file);
                    deleted++;
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[Retention] Could not delete {file}: {ex.Message}");
            }
        }

        if (deleted > 0)
            Log.Info($"[Retention] Deleted {deleted} old {label} file(s).");
    }
}
