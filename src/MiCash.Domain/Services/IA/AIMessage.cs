using MiCash.Domain.Enums;

namespace MiCash.Domain.Services.IA
{
    public class AIMessage
    {
        public AIMessageRole Role { get; set; } 
        public string Content { get; set; } = string.Empty;
    }
}
