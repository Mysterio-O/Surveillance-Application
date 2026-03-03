
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Surveil.Agent.Services;

public class ActivityService
{
    [DllImport("user32.dll")]
    static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [StructLayout(LayoutKind.Sequential)]
    struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    public bool IsIdle(TimeSpan idleThreshold)
    {
        uint idle = GetIdleTimeMs();
        return idle >= idleThreshold.TotalMilliseconds;
    }

    private static uint GetIdleTimeMs()
    {
        LASTINPUTINFO lastInPut = new LASTINPUTINFO();
        lastInPut.cbSize = (uint)Marshal.SizeOf(lastInPut);
        if (!GetLastInputInfo(ref lastInPut)) return 0;
        return ((uint)Environment.TickCount - lastInPut.dwTime);
    }
}
