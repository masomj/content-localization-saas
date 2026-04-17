using ContentLocalizationSaaS.Application.Abstractions;

namespace ContentLocalizationSaaS.Infrastructure.Services;

/// <summary>
/// Stub tone check service that returns no mismatch.
/// TODO: Replace with real LLM integration (OpenAI, Anthropic, etc.)
/// </summary>
public sealed class StubToneCheckService : IToneCheckService
{
    public Task<ToneCheckResponse> CheckAsync(string text, string toneDescription, string language)
    {
        // Stub: no LLM configured — always returns no mismatch
        return Task.FromResult(new ToneCheckResponse(
            HasMismatch: false,
            Suggestion: "",
            Confidence: 0,
            Reasoning: "Stub: no LLM configured"
        ));
    }
}
