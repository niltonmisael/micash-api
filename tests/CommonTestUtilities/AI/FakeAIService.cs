using MiCash.Domain.Services.IA;

namespace CommonTestUtilities.AI
{
    /// <summary>
    /// Deterministic IAIService for tests: never calls the real provider.
    /// </summary>
    public class FakeAIService : IAIService
    {
        public const string DEFAULT_RESPONSE = "Resposta de teste da MI.";

        private readonly System.Exception? _exceptionToThrow;

        public List<AIRequest> Requests { get; } = [];

        public FakeAIService ( System.Exception? exceptionToThrow = null )
        {
            _exceptionToThrow = exceptionToThrow;
        }

        public Task<AIResponse> GenerateResponseAsync ( AIRequest request , CancellationToken cancellationToken )
        {
            Requests.Add(request);

            if ( _exceptionToThrow is not null ) {
                throw _exceptionToThrow;
            }

            return Task.FromResult(new AIResponse { Content = DEFAULT_RESPONSE });
        }
    }
}
