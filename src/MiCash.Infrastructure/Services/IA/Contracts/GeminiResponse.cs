namespace MiCash.Infrastructure.Services.IA.Contracts
{
    public class GeminiResponse
    {
        public List<GeminiCandidate> Candidates { get; set; } = [];
    }
    public class GeminiCandidate
    {
        public GeminiContent Content { get; set; } = new();
        public string FinishReason { get; set; } = string.Empty;
    }
}
