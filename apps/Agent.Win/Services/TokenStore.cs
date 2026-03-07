using System.Security.Cryptography;
using System.Text;

namespace Surveil.Agent.Services;

/// <summary>Securely stores JWT tokens on disk using Windows DPAPI.</summary>
public static class TokenStore
{
    private static readonly string StorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SurveilWin", "agent_tokens.dat");

    public static void Save(string accessToken, string refreshToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StorePath)!);
        var data = Encoding.UTF8.GetBytes($"{accessToken}\n{refreshToken}");
        var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(StorePath, encrypted);
    }

    public static (string AccessToken, string RefreshToken)? Load()
    {
        if (!File.Exists(StorePath)) return null;
        try
        {
            var encrypted = File.ReadAllBytes(StorePath);
            var data = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            var parts = Encoding.UTF8.GetString(data).Split('\n');
            if (parts.Length < 2) return null;
            return (parts[0], parts[1]);
        }
        catch { return null; }
    }

    public static void Clear()
    {
        if (File.Exists(StorePath)) File.Delete(StorePath);
    }
}
