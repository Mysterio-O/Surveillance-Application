
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Drawing;
using Surveil.Utils;

namespace Surveil.Agent.Services;

public class EmbeddingService : IDisposable
{
    private readonly InferenceSession? _session;

    public EmbeddingService(string modelPath)
    {
        try
        {
            if (File.Exists(modelPath))
                _session = new InferenceSession(modelPath);
            else
                Log.Warn($"ONNX model not found at {modelPath}; embeddings disabled.");
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to load ONNX model: {ex.Message}");
        }
    }

    public float[]? Encode(Bitmap bmp)
    {
        if (_session is null) return null;
        // Resize to 224x224 and normalize to 0..1 (simple MVP; adjust for model's preprocessing)
        using var resized = new Bitmap(bmp, new Size(224, 224));
        var tensor = new DenseTensor<float>(new[] { 1, 3, 224, 224 });
        for (int y = 0; y < 224; y++)
        for (int x = 0; x < 224; x++)
        {
            var c = resized.GetPixel(x, y);
            tensor[0, 0, y, x] = c.R / 255f;
            tensor[0, 1, y, x] = c.G / 255f;
            tensor[0, 2, y, x] = c.B / 255f;
        }
        try
        {
            var inputs = new List<NamedOnnxValue>{ NamedOnnxValue.CreateFromTensor("input", tensor) };
            using var results = _session.Run(inputs);
            var first = results.First().AsTensor<float>();
            return first.ToArray();
        }
        catch (Exception ex)
        {
            Log.Warn($"Embedding inference failed: {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}
