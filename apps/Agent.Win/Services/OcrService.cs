using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using Surveil.Utils;

namespace Surveil.Agent.Services
{
    public class OcrService
    {
        public async Task<string> ExtractAsync(Bitmap bmp, string lang = "eng")
        {
            string tmp = Path.ChangeExtension(Path.GetTempFileName(), ".png");
            bmp.Save(tmp, ImageFormat.Png);

            // Build ProcessStartInfo explicitly (no object initializer)
            var args = $"\"{tmp}\" stdout -l {lang} --psm 6";
            var psi = new ProcessStartInfo("tesseract", args);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;

            try
            {
                using var p = Process.Start(psi);
                if (p == null) return string.Empty;

                string text = await p.StandardOutput.ReadToEndAsync();
                await p.WaitForExitAsync();
                return text.Trim();
            }
            catch (Exception ex)
            {
                Log.Warn($"Tesseract not found or failed: {ex.Message}");
                return string.Empty;
            }
            finally
            {
                try { File.Delete(tmp); } catch { /* ignore */ }
            }
        }
    }
}