using System.Text.Json.Serialization;

namespace MiCash.Infrastructure.Services.IA.Contracts
{
    public class GeminiRequest
    {
        public List<GeminiContent> Contents { get; set; } = [];
        public GeminiGenerationConfig GenerationConfig { get; set; } = new();
    }

    public class GeminiContent
    {
        public string Role { get; set; } = string.Empty;

        public List<GeminiPart> Parts { get; set; } = [];
    }

    public class GeminiPart
    {
        public string? Text { get; set; }

        /// <summary>
        /// True when the part is the model's reasoning summary (Gemini 3 thinking), not the answer.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Thought { get; set; }

        /// <summary>
        /// Opaque signature Gemini 3 may attach to a part (usually the last one).
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ThoughtSignature { get; set; }
    }

    public class GeminiGenerationConfig
    {
        public double Temperature { get; set; } = 1.0;

        public int MaxOutputTokens { get; set; } = 1024;

        public double TopP { get; set; } = 0.95;

        public int TopK { get; set; } = 40;
    }

}
