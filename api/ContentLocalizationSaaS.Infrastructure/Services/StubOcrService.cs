using ContentLocalizationSaaS.Application.Abstractions;

namespace ContentLocalizationSaaS.Infrastructure.Services;

/// <summary>
/// Stub OCR service that returns empty results.
/// TODO: Replace with real OCR integration (Azure Computer Vision, Google Cloud Vision, etc.)
/// </summary>
public sealed class StubOcrService : IOcrService
{
    public Task<List<OcrResult>> ProcessAsync(string imagePath)
    {
        // Stub: return empty results — no text regions detected
        return Task.FromResult(new List<OcrResult>());
    }
}
