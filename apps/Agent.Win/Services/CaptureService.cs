
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Surveil.Contracts;
using Surveil.Utils;

namespace Surveil.Agent.Services;

public class CaptureService
{
    // NOTE: For MVP we use GDI screenshot of foreground window as fallback
    // to avoid complex D3D setup in this sample. For production, switch to
    // Windows.Graphics.Capture (see README for details).

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;        // x position of upper-left corner
        public int Top;         // y position of upper-left corner
        public int Right;       // x position of lower-right corner
        public int Bottom;      // y position of lower-right corner
    }

    public Bitmap? CaptureForeground()
    {
        IntPtr handle = GetForegroundWindow();
        if (handle == IntPtr.Zero) return null;
        if (!GetWindowRect(handle, out RECT r)) return null;
        int width = r.Right - r.Left; int height = r.Bottom - r.Top;
        if (width <= 0 || height <= 0) return null;

        var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(r.Left, r.Top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        return bmp;
    }

    public string GetForegroundWindowTitle()
    {
        IntPtr handle = GetForegroundWindow();
        var sb = new System.Text.StringBuilder(1024);
        _ = GetWindowText(handle, sb, sb.Capacity);
        return sb.ToString();
    }
}
