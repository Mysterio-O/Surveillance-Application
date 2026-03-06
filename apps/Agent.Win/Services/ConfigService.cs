using System.Text.Json;
using Surveil.Contracts;
using Surveil.Utils;

namespace Surveil.Agent.Services;

/// <summary>
/// Loads <see cref="AppConfig"/> from appsettings.json, searching several
/// well-known locations. Falls back to defaults when no file is found.
/// </summary>
public class ConfigService
{
    private const string FileName = "appsettings.json";

    public AppConfig Config { get; }

    public ConfigService()
    {
        Config = Load();
    }

    // -----------------------------------------------------------------------
    // Loading
    // -----------------------------------------------------------------------

    private static AppConfig Load()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, FileName),
            // When run with `dotnet run` the BaseDirectory is deep in bin/debug
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", FileName),
            Path.Combine(Directory.GetCurrentDirectory(), FileName)
        };

        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        foreach (var path in candidates)
        {
            if (!File.Exists(path)) continue;
            try
            {
                var json = File.ReadAllText(path);
                var cfg  = JsonSerializer.Deserialize<AppConfig>(json, opts);
                if (cfg is not null)
                {
                    Log.Info($"[Config] Loaded from {path}");
                    return cfg;
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[Config] Failed to parse {path}: {ex.Message}");
            }
        }

        Log.Info("[Config] No appsettings.json found – using defaults.");
        return new AppConfig();
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Writes a pretty-printed default appsettings.json to <paramref name="directory"/>
    /// if one does not already exist there.
    /// </summary>
    public static void WriteDefaultIfMissing(string directory)
    {
        var path = Path.Combine(directory, FileName);
        if (File.Exists(path)) return;

        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(
            new AppConfig(),
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
        Log.Info($"[Config] Default config written to {path}");
    }
}
