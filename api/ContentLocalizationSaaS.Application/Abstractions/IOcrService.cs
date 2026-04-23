namespace ContentLocalizationSaaS.Application.Abstractions;

public sealed record OcrResult(string Text, double X, double Y, double Width, double Height, double Confidence);

public interface IOcrService
{
    /// <summary>
    /// Process an image and return detected text regions.
    /// TODO: Replace stub with real OCR integration (e.g. Azure Computer Vision, Google Cloud Vision).
    /// </summary>
    Task<List<OcrResult>> ProcessAsync(string imagePath);
}
