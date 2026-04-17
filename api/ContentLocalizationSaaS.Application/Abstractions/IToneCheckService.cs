namespace ContentLocalizationSaaS.Application.Abstractions;

public sealed record ToneCheckResponse(bool HasMismatch, string Suggestion, double Confidence, string Reasoning);

public interface IToneCheckService
{
    /// <summary>
    /// Check if text matches the expected tone.
    /// TODO: Replace stub with real LLM integration (OpenAI, Anthropic).
    /// </summary>
    Task<ToneCheckResponse> CheckAsync(string text, string toneDescription, string language);
}
