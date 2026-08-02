namespace MiCash.Infrastructure.Services.IA.Configuration
{
    public class GeminiSettings
    {
        public string ApiKey { get; set; } = string.Empty;

        public string Model { get; set; } = "gemini-2.5-flash";
        public string GenerateContentEndpoint =>
            $"models/{Model}:generateContent?key={ApiKey}";
    }
}
