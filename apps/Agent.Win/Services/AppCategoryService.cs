using System.Text.RegularExpressions;

namespace Surveil.Agent.Services;

/// <summary>Classifies an active application into a work category.</summary>
public static class AppCategoryService
{
    private static readonly Dictionary<string, string> ProcessCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        // Coding
        ["devenv.exe"]        = "coding",
        ["code.exe"]          = "coding",
        ["code"]              = "coding",
        ["rider64.exe"]       = "coding",
        ["idea64.exe"]        = "coding",
        ["pycharm64.exe"]     = "coding",
        ["webstorm64.exe"]    = "coding",
        ["notepad++.exe"]     = "coding",
        ["vim.exe"]           = "coding",
        ["nvim.exe"]          = "coding",
        ["neovide.exe"]       = "coding",
        ["sublime_text.exe"]  = "coding",
        ["atom.exe"]          = "coding",
        ["cursor.exe"]        = "coding",

        // Browser
        ["chrome.exe"]        = "browser",
        ["firefox.exe"]       = "browser",
        ["msedge.exe"]        = "browser",
        ["opera.exe"]         = "browser",
        ["brave.exe"]         = "browser",
        ["vivaldi.exe"]       = "browser",
        ["iexplore.exe"]      = "browser",

        // Docs
        ["winword.exe"]       = "docs",
        ["excel.exe"]         = "docs",
        ["powerpnt.exe"]      = "docs",
        ["soffice.exe"]       = "docs",
        ["notion.exe"]        = "docs",
        ["obsidian.exe"]      = "docs",
        ["onenote.exe"]       = "docs",
        ["acrord32.exe"]      = "docs",

        // Communication
        ["slack.exe"]         = "communication",
        ["teams.exe"]         = "communication",
        ["msteams.exe"]       = "communication",
        ["discord.exe"]       = "communication",
        ["zoom.exe"]          = "communication",
        ["webex.exe"]         = "communication",
        ["skype.exe"]         = "communication",
        ["telegram.exe"]      = "communication",
        ["signal.exe"]        = "communication",
        ["whatsapp.exe"]      = "communication",

        // Terminal
        ["windowsterminal.exe"] = "terminal",
        ["cmd.exe"]           = "terminal",
        ["powershell.exe"]    = "terminal",
        ["pwsh.exe"]          = "terminal",
        ["wsl.exe"]           = "terminal",
        ["ubuntu.exe"]        = "terminal",
        ["wt.exe"]            = "terminal",

        // Media
        ["vlc.exe"]           = "media",
        ["spotify.exe"]       = "media",
        ["wmplayer.exe"]      = "media",
        ["photos.exe"]        = "media",
        ["mspaint.exe"]       = "media",
        ["netflix.exe"]       = "media",

        // System
        ["explorer.exe"]      = "system",
        ["taskmgr.exe"]       = "system",
        ["control.exe"]       = "system",
        ["regedit.exe"]       = "system",
        ["mmc.exe"]           = "system",
        ["settings.exe"]      = "system",
    };

    private static readonly string[] WorkBrowserKeywords =
    {
        "github", "gitlab", "jira", "confluence", "notion", "figma", "linear",
        "trello", "google docs", "google sheets", "google slides", "stackoverflow",
        "docs.microsoft", "azure", "bitbucket", "asana", "monday.com", "clickup"
    };

    public static string Classify(string processName, string windowTitle, bool isIdle = false)
    {
        if (isIdle) return "idle";

        // Normalize process name: strip path, lowercase
        var proc = Path.GetFileName(processName).ToLowerInvariant();

        if (ProcessCategories.TryGetValue(proc, out var cat))
        {
            if (cat == "browser")
            {
                // Check if it's a work browser tab
                var titleLower = windowTitle.ToLowerInvariant();
                foreach (var kw in WorkBrowserKeywords)
                    if (titleLower.Contains(kw))
                        return "browser_work";
            }
            return cat;
        }

        // Title-based fallback
        var title = windowTitle.ToLowerInvariant();
        if (title.Contains("visual studio") || title.Contains("vs code") || title.Contains("intellij") || title.Contains("pycharm"))
            return "coding";
        if (title.Contains("slack") || title.Contains("teams") || title.Contains("zoom") || title.Contains("discord"))
            return "communication";
        if (title.Contains("word") || title.Contains("excel") || title.Contains("powerpoint") || title.Contains("notion") || title.Contains("obsidian"))
            return "docs";
        if (title.Contains("terminal") || title.Contains("command prompt") || title.Contains("powershell"))
            return "terminal";

        return "other";
    }

    public static string? ExtractUrlFromOcrText(string ocrText)
    {
        var regex = new Regex(@"https?://([^\s/]+)", RegexOptions.IgnoreCase);
        var match = regex.Match(ocrText);
        return match.Success ? match.Groups[1].Value.ToLowerInvariant() : null;
    }
}
