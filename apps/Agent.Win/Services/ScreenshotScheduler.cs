namespace Surveil.Agent.Services;

/// <summary>Determines when to capture screenshots based on category changes and time intervals.</summary>
public class ScreenshotScheduler
{
    private DateTime _lastScreenshot = DateTime.MinValue;
    private string? _lastCategory;

    public bool ShouldCapture(string category, int intervalMinutes)
    {
        // Always capture on category change
        if (category != _lastCategory)
        {
            _lastCategory = category;
            _lastScreenshot = DateTime.UtcNow;
            return true;
        }

        // Capture at configured interval
        if (DateTime.UtcNow - _lastScreenshot >= TimeSpan.FromMinutes(intervalMinutes))
        {
            _lastScreenshot = DateTime.UtcNow;
            return true;
        }

        return false;
    }
}
