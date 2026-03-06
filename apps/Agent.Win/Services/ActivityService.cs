using System.Runtime.InteropServices;

namespace Surveil.Agent.Services;

/// <summary>
/// Detects user activity: idle time and cursor movement.
/// </summary>
public class ActivityService
{
    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    private POINT _lastCursorPos;
    private bool  _firstPoll = true;

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>Returns true when no input has been received for at least <paramref name="idleThreshold"/>.</summary>
    public bool IsIdle(TimeSpan idleThreshold)
        => GetIdleTimeMs() >= (uint)idleThreshold.TotalMilliseconds;

    /// <summary>Returns the time elapsed since the last user input event.</summary>
    public TimeSpan GetIdleTime()
        => TimeSpan.FromMilliseconds(GetIdleTimeMs());

    /// <summary>Returns the current cursor position in virtual-screen coordinates.</summary>
    public (int X, int Y) GetCursorPosition()
    {
        GetCursorPos(out POINT pt);
        return (pt.X, pt.Y);
    }

    /// <summary>
    /// Returns true if the cursor has moved since the last call to this method.
    /// Always returns false on the very first poll (no baseline yet).
    /// </summary>
    public bool HasCursorMoved()
    {
        if (!GetCursorPos(out POINT current)) return false;

        if (_firstPoll)
        {
            _lastCursorPos = current;
            _firstPoll = false;
            return false;
        }

        bool moved = current.X != _lastCursorPos.X
                  || current.Y != _lastCursorPos.Y;
        _lastCursorPos = current;
        return moved;
    }

    // -----------------------------------------------------------------------
    // Implementation
    // -----------------------------------------------------------------------

    private static uint GetIdleTimeMs()
    {
        var lii = new LASTINPUTINFO
        {
            cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>()
        };
        if (!GetLastInputInfo(ref lii)) return 0;
        return (uint)Environment.TickCount - lii.dwTime;
    }
}
