using MiCash.Application.UseCases.WebHook.Handlers;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.WebHook;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MiCash.Application.UseCases.WebHook
{
    public class ProcessPagarMeWebhookUseCase : IProcessPagarMeWebhookUseCase
    {
        private readonly ILogger<ProcessPagarMeWebhookUseCase> _logger;
        private readonly IEnumerable<IPagarMeWebhookHandler> _handlers;
        private readonly IProcessedWebhookRepository _processedWebhookRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ProcessPagarMeWebhookUseCase (
            ILogger<ProcessPagarMeWebhookUseCase> logger,
            IEnumerable<IPagarMeWebhookHandler> handlers ,
            IProcessedWebhookRepository processedWebhookRepository,
            IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _handlers = handlers;
            _processedWebhookRepository = processedWebhookRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task ExecuteAsync (
            JsonElement payload ,
            CancellationToken cancellationToken )
        {
            var webhookId =
                payload.GetProperty("id")
                .GetString();

            var eventType =
                payload.GetProperty("type").GetString();

            if ( string.IsNullOrWhiteSpace(webhookId) ) {
                _logger.LogWarning(
                    "Webhook recebido sem ID.");

                return;
            }

            var alreadyProcessed =
                await _processedWebhookRepository
                    .ExistsByWebhookIdAsync(
                        webhookId ,
                        cancellationToken);

            if ( alreadyProcessed ) {
                _logger.LogInformation(
                    "Webhook {WebhookId} já processado." ,
                    webhookId);

                return;
            }

            var webhookLog =
                new ProcessedWebhook
                {
                    WebhookId = webhookId ,
                    EventType = eventType ?? string.Empty ,
                    Payload = payload.GetRawText() ,
                    ProcessedAt = DateTime.UtcNow
                };

            try {
                var handler =
                    _handlers.FirstOrDefault(
                        x => x.EventName == eventType);

                if ( handler is null ) {
                    webhookLog.Success = false;

                    webhookLog.Error =
                        $"Handler não encontrado para {eventType}";

                    await _processedWebhookRepository
                        .AddAsync(
                            webhookLog ,
                            cancellationToken);

                    await _unitOfWork.Commit(
                        cancellationToken);

                    return;
                }

                await handler.HandleAsync(
                    payload ,
                    cancellationToken);

                webhookLog.Success = true;

                await _processedWebhookRepository
                    .AddAsync(
                        webhookLog ,
                        cancellationToken);

                await _unitOfWork.Commit(cancellationToken);

            } catch ( System.Exception ex ) {

                webhookLog.Success = false;

                webhookLog.Error = ex.ToString();

                await _processedWebhookRepository
                    .AddAsync(
                        webhookLog ,
                        cancellationToken);

                await _unitOfWork.Commit(
                    cancellationToken);

                _logger.LogError(
                    ex ,
                    "Erro ao processar webhook {WebhookId}" ,
                    webhookId);

                throw;
            }
        }
    }
}
