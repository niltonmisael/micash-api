namespace MiCash.Infrastructure.Services.IA.Configuration
{
    public class GeminiSettings
    {
        public string ApiKey { get; set; } = string.Empty;

        public string Model { get; set; } = "gemini-3.1-flash-lite";
        public string GenerateContentEndpoint =>
            $"models/{Model}:generateContent?key={ApiKey}";
    }
}
