using MiCash.Application.Services.Subscriptions;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Services.Jobs;
using MiCash.Domain.Services.Payments;
using Microsoft.Extensions.Logging;

namespace MiCash.Application.Jobs.Subscriptions
{
    /// <summary>
    /// Daily safety net for lost webhooks and jobs:
    /// 1. Canceled subscriptions whose paid period is over but are still Active (expiration job lost) → Free.
    /// 2. Active subscriptions whose next billing date passed more than <see cref="GracePeriod"/> ago should have
    ///    been renewed (charge.paid) or canceled (subscription.canceled): their state is read from Pagar.me,
    ///    which is the source of truth for billing.
    /// </summary>
    public class SubscriptionReconciliationJob : ISubscriptionReconciliationJob
    {
        /// <summary>Time Pagar.me gets to charge the renewal and deliver its webhooks before we check.</summary>
        public static readonly TimeSpan GracePeriod = TimeSpan.FromDays(1);

        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;
        private readonly ISubscriptionsUpdateOnlyRepository _subscriptionsUpdateOnlyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<SubscriptionReconciliationJob> _logger;

        public SubscriptionReconciliationJob (
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository ,
            ISubscriptionsUpdateOnlyRepository subscriptionsUpdateOnlyRepository ,
            IUnitOfWork unitOfWork ,
            IPaymentGateway paymentGateway ,
            IBackgroundJobService backgroundJobService ,
            TimeProvider timeProvider ,
            ILogger<SubscriptionReconciliationJob> logger )
        {
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
            _subscriptionsUpdateOnlyRepository = subscriptionsUpdateOnlyRepository;
            _unitOfWork = unitOfWork;
            _paymentGateway = paymentGateway;
            _backgroundJobService = backgroundJobService;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task ExecuteAsync ( CancellationToken cancellationToken )
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            await ExpireEndedPeriodsAsync(now , cancellationToken);

            await CheckOverdueRenewalsAsync(now , cancellationToken);
        }

        private async Task ExpireEndedPeriodsAsync ( DateTime now , CancellationToken cancellationToken )
        {
            var ended = await _subscriptionsReadOnlyRepository.GetActiveEndedBeforeAsync(now , cancellationToken);

            foreach ( var subscription in ended ) {
                await SafelyAsync(subscription , () => {
                    if ( !string.IsNullOrWhiteSpace(subscription.CancellationJobId) ) {
                        _backgroundJobService.Delete(subscription.CancellationJobId);
                    }

                    SubscriptionCancellationRules.Close(subscription , subscription.EndsAt!.Value);

                    _logger.LogWarning(
                        "Subscription {SubscriptionId} com período pago encerrado em {EndsAt} ainda ativa (job perdido). Encerrada." ,
                        subscription.Id ,
                        subscription.EndsAt);

                    return Task.FromResult(true);
                } , now , cancellationToken);
            }
        }

        private async Task CheckOverdueRenewalsAsync ( DateTime now , CancellationToken cancellationToken )
        {
            var overdue =
                await _subscriptionsReadOnlyRepository
                    .GetActiveWithBillingDateBeforeAsync(
                        now - GracePeriod ,
                        cancellationToken);

            _logger.LogInformation(
                "Reconciliação de assinaturas: {Count} assinatura(s) com renovação não confirmada." ,
                overdue.Count);

            foreach ( var subscription in overdue ) {
                await SafelyAsync(subscription , async () => {
                    var remote =
                        await _paymentGateway.GetSubscriptionAsync(
                            subscription.PagarmeSubscriptionId! ,
                            cancellationToken);

                    if ( remote is null ) {
                        _logger.LogWarning(
                            "Subscription {SubscriptionId} ({PagarmeId}) não existe no Pagar.me." ,
                            subscription.Id ,
                            subscription.PagarmeSubscriptionId);

                        return false;
                    }

                    if ( remote.IsCanceled ) {
                        // The paid period is already over here (overdue), so this closes the subscription.
                        SubscriptionCancellationRules.ApplyGatewayCancellation(
                            subscription , remote.CanceledAt , now , _backgroundJobService);

                        _logger.LogWarning(
                            "Subscription {SubscriptionId} estava cancelada no Pagar.me (webhook perdido). Status={Status}" ,
                            subscription.Id ,
                            subscription.Status);

                        return true;
                    }

                    if ( remote.NextBillingAt is { } nextBillingAt &&
                         nextBillingAt > subscription.NextBillingDate ) {
                        subscription.NextBillingDate = nextBillingAt;

                        _logger.LogWarning(
                            "Subscription {SubscriptionId} renovada no Pagar.me (webhook perdido). Próxima cobrança: {NextBillingDate}" ,
                            subscription.Id ,
                            nextBillingAt);

                        return true;
                    }

                    // Charge still pending/retrying at Pagar.me: its charge.* webhooks settle the status.
                    _logger.LogWarning(
                        "Subscription {SubscriptionId} sem renovação confirmada. Status no Pagar.me: {RemoteStatus}" ,
                        subscription.Id ,
                        remote.Status);

                    return false;
                } , now , cancellationToken);
            }
        }

        /// <summary>Applies <paramref name="change"/> (true = save); one failing subscription never stops the others.</summary>
        private async Task SafelyAsync (
            Subscription subscription ,
            Func<Task<bool>> change ,
            DateTime now ,
            CancellationToken cancellationToken )
        {
            try {
                if ( !await change() ) {
                    return;
                }

                subscription.UpdatedAt = now;

                _subscriptionsUpdateOnlyRepository.Update(subscription , cancellationToken);

                await _unitOfWork.Commit(cancellationToken);

            } catch ( System.Exception ex ) when ( ex is not OperationCanceledException ) {
                _unitOfWork.DiscardChanges();

                _logger.LogError(
                    ex ,
                    "Erro ao reconciliar a Subscription {SubscriptionId}." ,
                    subscription.Id);
            }
        }
    }
}
