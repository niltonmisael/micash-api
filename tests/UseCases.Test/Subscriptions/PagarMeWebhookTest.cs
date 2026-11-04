using CommonTestUtilities.Jobs;
using CommonTestUtilities.Logger;
using CommonTestUtilities.Time;
using FluentAssertions;
using MiCash.Application.UseCases.WebHook;
using MiCash.Application.UseCases.WebHook.Handlers;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.PaymentLinks;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Repositories.WebHook;
using MiCash.Domain.Services.Payments;
using MiCash.Domain.Services.Payments.Models;
using MiCash.Domain.Services.Plans;
using Moq;
using System.Text.Json;

namespace UseCases.Test.Subscriptions
{
    public class PagarMeWebhookTest
    {
        private static JsonElement Json ( string json ) => JsonDocument.Parse(json).RootElement.Clone();

        private static JsonElement Event ( string type , string data , string id = "hook_1" ) =>
            Json($$"""{ "id": "{{id}}", "type": "{{type}}", "data": {{data}} }""");

        // ---------- ProcessPagarMeWebhookUseCase ----------

        private sealed class Orchestrator
        {
            public Mock<IPagarMeWebhookHandler> Handler { get; } = new();
            public Mock<IProcessedWebhookRepository> Logs { get; } = new();
            public Mock<IUnitOfWork> UnitOfWork { get; } = new();
            public List<ProcessedWebhook> Added { get; } = [];

            public Orchestrator ( ProcessedWebhook? existing = null )
            {
                Handler.SetupGet(h => h.EventName).Returns("charge.paid");
                Logs.Setup(l => l.GetByWebhookIdAsync("hook_1" , It.IsAny<CancellationToken>())).ReturnsAsync(existing);
                Logs.Setup(l => l.AddAsync(It.IsAny<ProcessedWebhook>() , It.IsAny<CancellationToken>()))
                    .Callback<ProcessedWebhook , CancellationToken>(( log , _ ) => Added.Add(log))
                    .Returns(Task.CompletedTask);
            }

            public ProcessPagarMeWebhookUseCase Build () => new(
                new LoggerServiceBuilder<ProcessPagarMeWebhookUseCase>().Build() ,
                [Handler.Object] ,
                Logs.Object ,
                UnitOfWork.Object);
        }

        [Fact]
        public async Task Success_Commits_Handler_Changes_And_Log_Together ()
        {
            var sut = new Orchestrator();

            await sut.Build().ExecuteAsync(Event("charge.paid" , "{}") , CancellationToken.None);

            sut.Handler.Verify(h => h.HandleAsync(It.IsAny<JsonElement>() , It.IsAny<CancellationToken>()) , Times.Once);
            sut.Added.Should().ContainSingle(l => l.Success && l.WebhookId == "hook_1");
            sut.UnitOfWork.Verify(u => u.Commit(It.IsAny<CancellationToken>()) , Times.Once);
        }

        [Fact]
        public async Task Event_Already_Processed_With_Success_Is_Ignored ()
        {
            var sut = new Orchestrator(new ProcessedWebhook { WebhookId = "hook_1" , Success = true });

            await sut.Build().ExecuteAsync(Event("charge.paid" , "{}") , CancellationToken.None);

            sut.Handler.Verify(h => h.HandleAsync(It.IsAny<JsonElement>() , It.IsAny<CancellationToken>()) , Times.Never);
            sut.UnitOfWork.Verify(u => u.Commit(It.IsAny<CancellationToken>()) , Times.Never);
        }

        [Fact]
        public async Task Event_That_Failed_Before_Is_Processed_Again_On_Retry ()
        {
            var previous = new ProcessedWebhook { WebhookId = "hook_1" , Success = false , Error = "boom" };
            var sut = new Orchestrator(previous);

            await sut.Build().ExecuteAsync(Event("charge.paid" , "{}") , CancellationToken.None);

            sut.Handler.Verify(h => h.HandleAsync(It.IsAny<JsonElement>() , It.IsAny<CancellationToken>()) , Times.Once);
            sut.Logs.Verify(l => l.Update(previous) , Times.Once);
            sut.Added.Should().BeEmpty();
            previous.Success.Should().BeTrue();
            previous.Error.Should().BeNull();
        }

        [Fact]
        public async Task Failure_Discards_Partial_Changes_Saves_Failure_And_Rethrows ()
        {
            var sut = new Orchestrator();
            sut.Handler.Setup(h => h.HandleAsync(It.IsAny<JsonElement>() , It.IsAny<CancellationToken>()))
                .ThrowsAsync(new WebhookNotReadyException("not yet"));

            var act = () => sut.Build().ExecuteAsync(Event("charge.paid" , "{}") , CancellationToken.None);

            await act.Should().ThrowAsync<WebhookNotReadyException>();
            sut.UnitOfWork.Verify(u => u.DiscardChanges() , Times.Once);
            sut.Added.Should().ContainSingle(l => !l.Success && l.Error!.Contains("not yet"));
            sut.UnitOfWork.Verify(u => u.Commit(It.IsAny<CancellationToken>()) , Times.Once);
        }

        // ---------- ChargePaidHandler ----------

        private sealed class Subscriptions
        {
            public Mock<ISubscriptionsReadOnlyRepository> Read { get; } = new();
            public Mock<ISubscriptionsUpdateOnlyRepository> Update { get; } = new();
            public Mock<IPaymentGateway> Gateway { get; } = new();

            public Subscriptions ( Subscription? subscription )
            {
                Read.Setup(r => r.GetByPagarmeSubscriptionIdAsync("sub_1" , It.IsAny<CancellationToken>()))
                    .ReturnsAsync(subscription);
            }

            public ChargePaidHandler ChargePaid () => new(
                Read.Object , Update.Object , Gateway.Object , new LoggerServiceBuilder<ChargePaidHandler>().Build());

            public ChargePaymentFailedHandler PaymentFailed () => new(
                Read.Object , Update.Object , new LoggerServiceBuilder<ChargePaymentFailedHandler>().Build());

            public SubscriptionCanceledHandler Canceled ( FakeBackgroundJobService jobs ) => new(
                Read.Object , Update.Object , jobs , new LoggerServiceBuilder<SubscriptionCanceledHandler>().Build());

            public ChargeRefundedHandler Refunded ( FakeBackgroundJobService jobs , DateTime now ) => new(
                Read.Object , Update.Object , Gateway.Object , jobs , new MutableTimeProvider(new DateTimeOffset(now)) ,
                new LoggerServiceBuilder<ChargeRefundedHandler>().Build());

            public ChargeChargedbackHandler Chargedback ( FakeBackgroundJobService jobs , DateTime now ) => new(
                Read.Object , Update.Object , Gateway.Object , jobs , new MutableTimeProvider(new DateTimeOffset(now)) ,
                new LoggerServiceBuilder<ChargeRefundedHandler>().Build());
        }

        private const string ChargeData =
            """{ "id": "ch_2", "paid_at": "2026-12-03T10:00:00Z", "invoice": { "subscriptionId": "sub_1" } }""";

        [Fact]
        public async Task Renewal_Of_An_Active_Subscription_Refreshes_The_Next_Billing_Date ()
        {
            var subscription = new Subscription
            {
                Status = SubscriptionStatus.Active ,
                PagarmeChargeId = "ch_1" ,
                StartsAt = new DateTime(2026 , 11 , 3 , 0 , 0 , 0 , DateTimeKind.Utc) ,
                NextBillingDate = new DateTime(2026 , 12 , 3 , 0 , 0 , 0 , DateTimeKind.Utc)
            };
            var sut = new Subscriptions(subscription);
            sut.Gateway.Setup(g => g.GetSubscriptionAsync("sub_1" , It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GatewaySubscription { Status = "active" , NextBillingAt = new DateTime(2027 , 1 , 3 , 0 , 0 , 0 , DateTimeKind.Utc) });

            await sut.ChargePaid().HandleAsync(Event("charge.paid" , ChargeData) , CancellationToken.None);

            subscription.NextBillingDate.Should().Be(new DateTime(2027 , 1 , 3 , 0 , 0 , 0 , DateTimeKind.Utc));
            subscription.PagarmeChargeId.Should().Be("ch_2");
            subscription.Status.Should().Be(SubscriptionStatus.Active);
            sut.Update.Verify(u => u.Update(subscription , It.IsAny<CancellationToken>()) , Times.Once);
        }

        [Fact]
        public async Task First_Charge_Activates_Even_When_The_Gateway_Api_Is_Down ()
        {
            var subscription = new Subscription { Status = SubscriptionStatus.Pending };
            var sut = new Subscriptions(subscription);
            sut.Gateway.Setup(g => g.GetSubscriptionAsync(It.IsAny<string>() , It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("down"));

            await sut.ChargePaid().HandleAsync(Event("charge.paid" , ChargeData) , CancellationToken.None);

            subscription.Status.Should().Be(SubscriptionStatus.Active);
            subscription.StartsAt.Should().Be(new DateTime(2026 , 12 , 3 , 10 , 0 , 0 , DateTimeKind.Utc));
        }

        [Fact]
        public async Task Charge_Paid_Before_Subscription_Created_Asks_For_Retry ()
        {
            var sut = new Subscriptions(subscription: null);

            var act = () => sut.ChargePaid().HandleAsync(Event("charge.paid" , ChargeData) , CancellationToken.None);

            await act.Should().ThrowAsync<WebhookNotReadyException>();
        }

        [Fact]
        public async Task Charge_Paid_Does_Not_Reopen_A_Cancelled_Subscription ()
        {
            var subscription = new Subscription { Status = SubscriptionStatus.Cancelled };
            var sut = new Subscriptions(subscription);

            await sut.ChargePaid().HandleAsync(Event("charge.paid" , ChargeData) , CancellationToken.None);

            subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
            sut.Update.Verify(u => u.Update(It.IsAny<Subscription>() , It.IsAny<CancellationToken>()) , Times.Never);
        }

        [Fact]
        public async Task Payment_Failed_Before_Subscription_Created_Asks_For_Retry ()
        {
            var sut = new Subscriptions(subscription: null);

            var act = () => sut.PaymentFailed().HandleAsync(Event("charge.payment_failed" , ChargeData) , CancellationToken.None);

            await act.Should().ThrowAsync<WebhookNotReadyException>();
        }

        [Fact]
        public async Task Payment_Failed_Without_Gateway_Errors_Still_Marks_The_Failure ()
        {
            var subscription = new Subscription { Status = SubscriptionStatus.Active };
            var sut = new Subscriptions(subscription);

            await sut.PaymentFailed().HandleAsync(
                Event("charge.payment_failed" , """{ "id": "ch_3", "invoice": { "subscriptionId": "sub_1" }, "last_transaction": null }""") ,
                CancellationToken.None);

            subscription.Status.Should().Be(SubscriptionStatus.PaymentFailed);
        }

        [Fact]
        public async Task Canceled_At_PagarMe_Keeps_Premium_Until_The_Paid_Period_Ends ()
        {
            // E.g. canceled at the Pagar.me dashboard on day 10 of a cycle paid until day 1 of next month.
            var paidUntil = DateTime.UtcNow.AddDays(20);
            var subscription = new Subscription
            {
                Id = 10 ,
                Status = SubscriptionStatus.Active ,
                NextBillingDate = paidUntil ,
                CancelAtPeriodEnd = true ,
                CancellationJobId = "job-1"   // pending "stop charges" job: no longer needed
            };
            var sut = new Subscriptions(subscription);
            var jobs = new FakeBackgroundJobService();

            await sut.Canceled(jobs).HandleAsync(
                Event("subscription.canceled" , """{ "id": "sub_1", "canceled_at": "2026-11-10T12:00:00Z" }""") ,
                CancellationToken.None);

            subscription.Status.Should().Be(SubscriptionStatus.Active);
            subscription.EndsAt.Should().Be(paidUntil);
            subscription.CancellationScheduledFor.Should().Be(paidUntil);
            jobs.ScheduledExpirations.Should().ContainSingle().Which.Should().Be((10L, paidUntil));
            subscription.CancellationJobId.Should().Be("expire-10");
        }

        [Fact]
        public async Task Canceled_Twice_Does_Not_Schedule_A_Second_Expiration ()
        {
            var paidUntil = DateTime.UtcNow.AddHours(5);
            var subscription = new Subscription
            {
                Id = 10 , Status = SubscriptionStatus.Active , NextBillingDate = paidUntil ,
                CancelAtPeriodEnd = true , EndsAt = paidUntil , CancellationJobId = "expire-10"
            };
            var sut = new Subscriptions(subscription);
            var jobs = new FakeBackgroundJobService();

            await sut.Canceled(jobs).HandleAsync(
                Event("subscription.canceled" , """{ "id": "sub_1" }""") , CancellationToken.None);

            jobs.ScheduledExpirations.Should().BeEmpty();
            subscription.CancellationJobId.Should().Be("expire-10");
        }

        [Fact]
        public async Task Canceled_Without_A_Paid_Period_And_Null_Canceled_At_Ends_Now ()
        {
            var subscription = new Subscription
            {
                Status = SubscriptionStatus.PaymentFailed ,
                NextBillingDate = DateTime.UtcNow.AddDays(20) ,
                CancelAtPeriodEnd = true ,
                CancellationJobId = "job-1"
            };
            var sut = new Subscriptions(subscription);

            await sut.Canceled(new FakeBackgroundJobService()).HandleAsync(
                Event("subscription.canceled" , """{ "id": "sub_1", "canceled_at": null }""") ,
                CancellationToken.None);

            subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
            subscription.EndsAt.Should().NotBeNull();
            subscription.CancellationJobId.Should().BeNull();
        }

        // ---------- Renewal refused: 3-day grace ----------

        [Fact]
        public async Task Refused_Renewal_Keeps_Premium_For_Three_Days_Counted_From_The_First_Failure ()
        {
            var subscription = new Subscription { Status = SubscriptionStatus.Active };
            var sut = new Subscriptions(subscription);
            var before = DateTime.UtcNow;

            await sut.PaymentFailed().HandleAsync(Event("charge.payment_failed" , ChargeData) , CancellationToken.None);

            subscription.Status.Should().Be(SubscriptionStatus.PaymentFailed);
            subscription.PaymentGraceUntil.Should().BeOnOrAfter(before + SubscriptionAccess.RenewalGracePeriod)
                .And.BeOnOrBefore(DateTime.UtcNow + SubscriptionAccess.RenewalGracePeriod);
            SubscriptionAccess.HasPremium(subscription , DateTime.UtcNow).Should().BeTrue();
            SubscriptionAccess.HasPremium(subscription , subscription.PaymentGraceUntil!.Value).Should().BeFalse("the grace ends after 3 days");

            // Pagar.me retries and fails again: the grace is not extended.
            var graceUntil = subscription.PaymentGraceUntil;
            await sut.PaymentFailed().HandleAsync(Event("charge.payment_failed" , ChargeData) , CancellationToken.None);
            subscription.PaymentGraceUntil.Should().Be(graceUntil);
        }

        [Fact]
        public async Task Refused_First_Charge_Gets_No_Grace ()
        {
            var subscription = new Subscription { Status = SubscriptionStatus.Pending };
            var sut = new Subscriptions(subscription);

            await sut.PaymentFailed().HandleAsync(Event("charge.payment_failed" , ChargeData) , CancellationToken.None);

            subscription.PaymentGraceUntil.Should().BeNull();
            SubscriptionAccess.HasPremium(subscription , DateTime.UtcNow).Should().BeFalse();
        }

        [Fact]
        public async Task Renewal_Paid_On_Retry_Makes_It_Active_Again_And_Ends_The_Grace ()
        {
            var subscription = new Subscription
            {
                Status = SubscriptionStatus.PaymentFailed ,
                PaymentGraceUntil = DateTime.UtcNow.AddDays(2)
            };
            var sut = new Subscriptions(subscription);

            await sut.ChargePaid().HandleAsync(Event("charge.paid" , ChargeData) , CancellationToken.None);

            subscription.Status.Should().Be(SubscriptionStatus.Active);
            subscription.PaymentGraceUntil.Should().BeNull();
        }

        // ---------- Refund / chargeback: Premium ends now ----------

        private static readonly DateTime RefundedAt = new(2026 , 11 , 15 , 14 , 0 , 0 , DateTimeKind.Utc);

        [Fact]
        public async Task Refund_Cancels_At_PagarMe_And_Ends_Premium_Now_Even_Inside_The_Paid_Period ()
        {
            var subscription = new Subscription
            {
                Id = 10 ,
                PagarmeSubscriptionId = "sub_1" ,
                Status = SubscriptionStatus.Active ,
                NextBillingDate = RefundedAt.AddDays(16) ,
                CancelAtPeriodEnd = true ,
                CancellationJobId = "job-1"
            };
            var sut = new Subscriptions(subscription);

            await sut.Refunded(new FakeBackgroundJobService() , RefundedAt)
                .HandleAsync(Event("charge.refunded" , ChargeData) , CancellationToken.None);

            sut.Gateway.Verify(g => g.CancelSubscriptionAsync("sub_1" , It.IsAny<CancellationToken>()) , Times.Once);
            subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
            subscription.EndsAt.Should().Be(RefundedAt);
            subscription.CancellationJobId.Should().BeNull("the scheduled job is no longer needed");
            SubscriptionAccess.HasPremium(subscription , RefundedAt).Should().BeFalse();
            sut.Update.Verify(u => u.Update(subscription , It.IsAny<CancellationToken>()) , Times.Once);
        }

        [Fact]
        public async Task Chargeback_Has_The_Same_Effect_As_A_Refund ()
        {
            var subscription = new Subscription
            {
                Status = SubscriptionStatus.PaymentFailed ,
                PaymentGraceUntil = RefundedAt.AddDays(1)
            };
            var sut = new Subscriptions(subscription);
            var handler = sut.Chargedback(new FakeBackgroundJobService() , RefundedAt);

            handler.EventName.Should().Be("charge.chargedback");
            await handler.HandleAsync(Event("charge.chargedback" , ChargeData) , CancellationToken.None);

            subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
            subscription.PaymentGraceUntil.Should().BeNull();
            SubscriptionAccess.HasPremium(subscription , RefundedAt).Should().BeFalse();
        }

        [Fact]
        public async Task Refund_Is_Retried_When_The_Gateway_Cancellation_Fails_And_Nothing_Changes ()
        {
            var subscription = new Subscription { Status = SubscriptionStatus.Active };
            var sut = new Subscriptions(subscription);
            sut.Gateway.Setup(g => g.CancelSubscriptionAsync("sub_1" , It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("down"));

            var act = () => sut.Refunded(new FakeBackgroundJobService() , RefundedAt)
                .HandleAsync(Event("charge.refunded" , ChargeData) , CancellationToken.None);

            await act.Should().ThrowAsync<HttpRequestException>("the webhook answers non-2xx and Pagar.me delivers it again");
            subscription.Status.Should().Be(SubscriptionStatus.Active);
        }

        [Fact]
        public async Task Refund_Of_An_Already_Closed_Subscription_Is_Ignored ()
        {
            var subscription = new Subscription { Status = SubscriptionStatus.Cancelled };
            var sut = new Subscriptions(subscription);

            await sut.Refunded(new FakeBackgroundJobService() , RefundedAt)
                .HandleAsync(Event("charge.refunded" , ChargeData) , CancellationToken.None);

            sut.Gateway.Verify(g => g.CancelSubscriptionAsync(It.IsAny<string>() , It.IsAny<CancellationToken>()) , Times.Never);
            sut.Update.Verify(u => u.Update(It.IsAny<Subscription>() , It.IsAny<CancellationToken>()) , Times.Never);
        }

        // ---------- SubscriptionCreatedHandler ----------

        [Fact]
        public async Task Subscription_Created_Stores_Gateway_Dates_As_Utc_Regardless_Of_Server_Time_Zone ()
        {
            var read = new Mock<ISubscriptionsReadOnlyRepository>();
            var write = new Mock<ISubscriptionsWriteOnlyRepository>();
            var links = new Mock<IPaymentLinkRepository>();
            var users = new Mock<IUserReadOnlyRepository>();
            Subscription? added = null;

            links.Setup(l => l.GetByPagarmePaymentLinkIdAsync("pl_1" , It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymentLink { UserId = 7 , PlanId = 2 });
            users.Setup(u => u.GetByIdAsync(7 , It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { Id = 7 });
            write.Setup(w => w.AddAsync(It.IsAny<Subscription>() , It.IsAny<CancellationToken>()))
                .Callback<Subscription , CancellationToken>(( s , _ ) => added = s)
                .Returns(Task.CompletedTask);

            var handler = new SubscriptionCreatedHandler(
                new Mock<IUserUpdateOnlyRepository>().Object ,
                users.Object ,
                write.Object ,
                read.Object ,
                links.Object ,
                new LoggerServiceBuilder<SubscriptionCreatedHandler>().Build());

            await handler.HandleAsync(
                Event("subscription.created" , """
                    {
                      "id": "sub_1", "code": "pl_1", "customer": { "id": "cus_1" },
                      "start_at": "2026-11-03T00:00:00Z", "next_billing_at": "2026-12-02T21:00:00-03:00"
                    }
                    """) ,
                CancellationToken.None);

            added.Should().NotBeNull();
            added!.Status.Should().Be(SubscriptionStatus.Pending);
            added.UserId.Should().Be(7);
            added.StartsAt.Should().Be(new DateTime(2026 , 11 , 3 , 0 , 0 , 0 , DateTimeKind.Utc));
            added.StartsAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
            added.NextBillingDate.Should().Be(new DateTime(2026 , 12 , 3 , 0 , 0 , 0 , DateTimeKind.Utc));
        }
    }
}
