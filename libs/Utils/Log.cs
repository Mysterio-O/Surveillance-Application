
namespace Surveil.Utils;

public static class Log
{
    private static readonly object _lock = new();
    public static void Info(string msg)
    {
        lock(_lock)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss} {msg}");
            Console.ResetColor();
        }
    }
    public static void Warn(string msg)
    {
        lock(_lock)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARN] {DateTime.Now:HH:mm:ss} {msg}");
            Console.ResetColor();
        }
    }
    public static void Error(string msg)
    {
        lock(_lock)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERR ] {DateTime.Now:HH:mm:ss} {msg}");
            Console.ResetColor();
        }
    }
}
