using SurveilWin.Api.Data.Entities;

namespace SurveilWin.Api.Services;

public class ProductivityScorerService
{
    public double Score(IEnumerable<ActivityFrame> frames, OrgPolicy? policy = null)
    {
        var frameList = frames.ToList();
        if (frameList.Count == 0) return 0.0;

        double productiveSeconds = 0;
        double totalSeconds = frameList.Count;

        foreach (var frame in frameList)
        {
            productiveSeconds += frame.AppCategory switch
            {
                "coding"        => 1.0,
                "docs"          => 1.0,
                "terminal"      => 1.0,
                "browser_work"  => 0.9,
                "communication" => 0.8,
                "browser"       => 0.5,
                "system"        => 0.3,
                "media"         => 0.1,
                "idle"          => 0.0,
                _               => 0.4
            };
        }

        return Math.Round(productiveSeconds / totalSeconds, 2);
    }
}
