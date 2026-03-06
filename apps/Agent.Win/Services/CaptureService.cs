using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using Surveil.Contracts;
using Surveil.Utils;

namespace Surveil.Agent.Services;

/// <summary>
/// Captures the screen on which the cursor currently resides (multi-monitor aware).
/// Falls back to the active foreground window when cursor position cannot be determined.
/// </summary>
public class CaptureService
{
    // -----------------------------------------------------------------------
    // Win32 P/Invoke declarations
    // -----------------------------------------------------------------------

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(
        IntPtr hdc, IntPtr lprcClip,
        MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    // -----------------------------------------------------------------------
    // Win32 structs and constants
    // -----------------------------------------------------------------------

    private delegate bool MonitorEnumProc(
        IntPtr hMonitor, IntPtr hdcMonitor,
        ref RECT lprcMonitor, IntPtr dwData);

    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;
    private const uint MONITORINFOF_PRIMARY      = 0x00000001;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
        public int Width  => Right  - Left;
        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFOEX
    {
        public uint   cbSize;
        public RECT   rcMonitor;
        public RECT   rcWork;
        public uint   dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns info for every connected monitor ordered by index.
    /// </summary>
    public List<MonitorInfo> GetAllMonitors()
    {
        var monitors = new List<MonitorInfo>();
        int index = 0;

        // Local function used instead of a lambda to satisfy the ref RECT parameter
        // requirement of the MonitorEnumProc delegate signature.
        bool Callback(IntPtr hMon, IntPtr hdcMon, ref RECT rect, IntPtr dwData)
        {
            var mi = new MONITORINFOEX
            {
                cbSize = (uint)Marshal.SizeOf<MONITORINFOEX>()
            };
            if (GetMonitorInfo(hMon, ref mi))
            {
                monitors.Add(new MonitorInfo(
                    Index:      index++,
                    DeviceName: mi.szDevice.TrimEnd('\0'),
                    Bounds: new ScreenRect(
                        mi.rcMonitor.Left,
                        mi.rcMonitor.Top,
                        mi.rcMonitor.Width,
                        mi.rcMonitor.Height),
                    IsPrimary: (mi.dwFlags & MONITORINFOF_PRIMARY) != 0
                ));
            }
            return true;
        }

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, Callback, IntPtr.Zero);
        return monitors;
    }

    /// <summary>
    /// Returns the monitor that the cursor currently occupies.
    /// Returns null only if the Win32 call fails entirely.
    /// </summary>
    public MonitorInfo? GetCursorMonitor()
    {
        if (!GetCursorPos(out POINT pt)) return null;

        IntPtr hMon = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
        var mi = new MONITORINFOEX
        {
            cbSize = (uint)Marshal.SizeOf<MONITORINFOEX>()
        };

        if (!GetMonitorInfo(hMon, ref mi)) return null;

        return new MonitorInfo(
            Index:      0,
            DeviceName: mi.szDevice.TrimEnd('\0'),
            Bounds: new ScreenRect(
                mi.rcMonitor.Left,
                mi.rcMonitor.Top,
                mi.rcMonitor.Width,
                mi.rcMonitor.Height),
            IsPrimary: (mi.dwFlags & MONITORINFOF_PRIMARY) != 0
        );
    }

    /// <summary>
    /// Returns the current cursor position in virtual-screen coordinates.
    /// </summary>
    public (int X, int Y) GetCursorPosition()
    {
        GetCursorPos(out POINT pt);
        return (pt.X, pt.Y);
    }

    /// <summary>
    /// Captures the entire screen on which the cursor currently resides.
    /// This is the primary capture method for multi-monitor tracking.
    /// </summary>
    /// <returns>
    /// A <see cref="Bitmap"/> of the cursor's screen and the corresponding
    /// <see cref="MonitorInfo"/>. Both can be null on failure.
    /// </returns>
    public (Bitmap? Bmp, MonitorInfo? Monitor) CaptureCursorScreen()
    {
        var monitor = GetCursorMonitor();
        if (monitor is null)
        {
            Log.Warn("CaptureCursorScreen: could not determine cursor monitor; falling back to foreground.");
            return (CaptureForeground(), null);
        }

        var r = monitor.Bounds;
        if (r.Width <= 0 || r.Height <= 0) return (null, monitor);

        try
        {
            var bmp = new Bitmap(r.Width, r.Height, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(r.Left, r.Top, 0, 0,
                new Size(r.Width, r.Height),
                CopyPixelOperation.SourceCopy);
            return (bmp, monitor);
        }
        catch (Exception ex)
        {
            Log.Error($"CaptureCursorScreen failed: {ex.Message}");
            return (null, monitor);
        }
    }

    /// <summary>
    /// Legacy: captures only the foreground window (single-monitor, no cursor tracking).
    /// Kept for fallback and backwards-compatibility.
    /// </summary>
    public Bitmap? CaptureForeground()
    {
        IntPtr handle = GetForegroundWindow();
        if (handle == IntPtr.Zero) return null;
        if (!GetWindowRect(handle, out RECT r)) return null;

        int w = r.Width;
        int h = r.Height;
        if (w <= 0 || h <= 0) return null;

        try
        {
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(r.Left, r.Top, 0, 0,
                new Size(w, h),
                CopyPixelOperation.SourceCopy);
            return bmp;
        }
        catch (Exception ex)
        {
            Log.Error($"CaptureForeground failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>Returns the title of the current foreground window.</summary>
    public string GetForegroundWindowTitle()
    {
        IntPtr handle = GetForegroundWindow();
        if (handle == IntPtr.Zero) return string.Empty;

        var sb = new StringBuilder(1024);
        GetWindowText(handle, sb, sb.Capacity);
        return sb.ToString();
    }

    /// <summary>Returns the process name of the current foreground window's owner.</summary>
    public string GetActiveProcessName()
    {
        IntPtr handle = GetForegroundWindow();
        if (handle == IntPtr.Zero) return string.Empty;

        GetWindowThreadProcessId(handle, out uint pid);
        try
        {
            return Process.GetProcessById((int)pid).ProcessName;
        }
        catch
        {
            return string.Empty;
        }
    }
}
