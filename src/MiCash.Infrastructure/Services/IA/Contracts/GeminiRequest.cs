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
        public string Text { get; set; } = string.Empty;
    }

    public class GeminiGenerationConfig
    {
        public double Temperature { get; set; } = 0.4;

        public int MaxOutputTokens { get; set; } = 1024;

        public double TopP { get; set; } = 0.95;

        public int TopK { get; set; } = 40;
    }

}
