using Microsoft.VisualBasic.FileIO;

namespace MiCash.Domain.Services.IA
{
    public class AIRequest
    {
        public IList<AIMessage> Messages { get; set; } = [];
        public AIOptions Options { get; set; } = new();
    }

    public class AIOptions
    {
        public double Temperature { get; set; } = 1.0;

        public int MaxOutputTokens { get; set; } = 1024;

        public double TopP { get; set; } = 0.95;
    }
}
