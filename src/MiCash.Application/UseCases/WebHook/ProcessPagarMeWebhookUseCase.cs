using MiCash.Application.UseCases.WebHook.Handlers;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.WebHook;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MiCash.Application.UseCases.WebHook
{
    /// <summary>
    /// Entry point of every Pagar.me webhook.
    /// - Idempotent: an event already processed with success is ignored; a failed one is processed again when
    ///   Pagar.me retries it (failures answer non-2xx, which is what makes Pagar.me retry).
    /// - Atomic: the handler's changes and the webhook log are committed together. On failure the handler's
    ///   partial changes are discarded and only the failure log is saved.
    /// </summary>
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
            var webhookId = WebhookPayload.GetString(payload , "id");
            var eventType = WebhookPayload.GetString(payload , "type") ?? string.Empty;

            if ( string.IsNullOrWhiteSpace(webhookId) ) {
                _logger.LogWarning("Webhook recebido sem ID. Tipo={EventType}" , eventType);

                return;
            }

            var existing =
                await _processedWebhookRepository
                    .GetByWebhookIdAsync(
                        webhookId ,
                        cancellationToken);

            if ( existing is { Success: true } ) {
                _logger.LogInformation(
                    "Webhook {WebhookId} já processado." ,
                    webhookId);

                return;
            }

            var rawPayload = payload.GetRawText();

            var handler =
                _handlers.FirstOrDefault(
                    x => x.EventName == eventType);

            if ( handler is null ) {
                // Not an error for Pagar.me (answers 200): the event is just not used by MiCash.
                await SaveLogAsync(existing , webhookId , eventType , rawPayload ,
                    success: false , error: $"Handler não encontrado para {eventType}" , cancellationToken);

                await _unitOfWork.Commit(cancellationToken);

                return;
            }

            try {
                await handler.HandleAsync(
                    payload ,
                    cancellationToken);

                await SaveLogAsync(existing , webhookId , eventType , rawPayload ,
                    success: true , error: null , cancellationToken);

                await _unitOfWork.Commit(cancellationToken);

            } catch ( System.Exception ex ) {

                if ( ex is WebhookNotReadyException ) {
                    _logger.LogWarning(
                        "Webhook {WebhookId} ({EventType}) adiado: {Reason}. O Pagar.me fará nova tentativa." ,
                        webhookId ,
                        eventType ,
                        ex.Message);
                } else {
                    _logger.LogError(
                        ex ,
                        "Erro ao processar webhook {WebhookId} ({EventType})" ,
                        webhookId ,
                        eventType);
                }

                // Never persist half of what the handler changed.
                _unitOfWork.DiscardChanges();

                await SaveLogAsync(existing , webhookId , eventType , rawPayload ,
                    success: false , error: ex.ToString() , cancellationToken);

                await _unitOfWork.Commit(cancellationToken);

                throw;
            }
        }

        private async Task SaveLogAsync (
            ProcessedWebhook? existing ,
            string webhookId ,
            string eventType ,
            string payload ,
            bool success ,
            string? error ,
            CancellationToken cancellationToken )
        {
            var log = existing ?? new ProcessedWebhook { WebhookId = webhookId };

            log.EventType = eventType;
            log.Payload = payload;
            log.Success = success;
            log.Error = error;
            log.ProcessedAt = DateTime.UtcNow;

            if ( existing is null ) {
                await _processedWebhookRepository.AddAsync(log , cancellationToken);
            } else {
                _processedWebhookRepository.Update(log);
            }
        }
    }
}
