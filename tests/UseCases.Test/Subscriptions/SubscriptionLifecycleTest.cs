using CommonTestUtilities.Jobs;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Logger;
using CommonTestUtilities.Time;
using FluentAssertions;
using MiCash.Application.Jobs.Subscriptions;
using MiCash.Application.UseCases.Checkout;
using MiCash.Application.UseCases.Subscriptions.Cancel;
using MiCash.Communication.Requests.Checkout;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.PaymentLinks;
using MiCash.Domain.Repositories.Plans;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Services.Jobs;
using MiCash.Domain.Services.Payments;
using MiCash.Domain.Services.Payments.Models;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using Moq;

namespace UseCases.Test.Subscriptions
{
    public class SubscriptionLifecycleTest
    {
        private static readonly DateTime NextBilling = new(2026 , 12 , 3 , 0 , 0 , 0 , DateTimeKind.Utc);

        // ---------- Cancellation request ----------

        private static (CancelSubscriptionUseCase useCase, Mock<IBackgroundJobService> jobs) CancelUseCase ( Subscription? subscription )
        {
            var read = new Mock<ISubscriptionsReadOnlyRepository>();
            read.Setup(r => r.GetActiveByUserIdAsync(1 , It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

            var jobs = new Mock<IBackgroundJobService>();
            jobs.Setup(j => j.ScheduleSubscriptionCancellation(It.IsAny<long>() , It.IsAny<DateTime>())).Returns("job-1");

            var useCase = new CancelSubscriptionUseCase(
                LoggedUserBuilder.Build(new User { Id = 1 }) ,
                read.Object ,
                new Mock<ISubscriptionsUpdateOnlyRepository>().Object ,
                new Mock<IUnitOfWork>().Object ,
                jobs.Object);

            return (useCase, jobs);
        }

        [Fact]
        public async Task Cancellation_Stops_Charges_Before_Billing_But_Keeps_Access_Until_The_Paid_Period_Ends ()
        {
            var paidUntil = NextBilling.AddYears(1);
            var subscription = new Subscription { Id = 10 , Status = SubscriptionStatus.Active , NextBillingDate = paidUntil };
            var (useCase, jobs) = CancelUseCase(subscription);

            await useCase.ExecuteAsync(CancellationToken.None);

            // Pagar.me is told a few hours earlier, so the next cycle is not charged...
            jobs.Verify(j => j.ScheduleSubscriptionCancellation(10 , paidUntil - CancelSubscriptionUseCase.CancellationLeadTime) , Times.Once);
            // ...but the user is promised (and keeps) Premium until the end of the paid period.
            subscription.CancellationScheduledFor.Should().Be(paidUntil);
            subscription.Status.Should().Be(SubscriptionStatus.Active);
            subscription.CancelAtPeriodEnd.Should().BeTrue();
            subscription.CancellationJobId.Should().Be("job-1");
        }

        [Fact]
        public async Task Cancellation_Without_Billing_Date_Is_A_Bad_Request_Not_A_Crash ()
        {
            var (useCase, _) = CancelUseCase(new Subscription { Status = SubscriptionStatus.Active , NextBillingDate = null });

            var act = () => useCase.ExecuteAsync(CancellationToken.None);

            (await act.Should().ThrowAsync<BadRequestException>())
                .Which.GetErrors().Should().Contain(ResourceErrorMessages.SUBSCRIPTION_BILLING_DATE_UNAVAILABLE);
        }

        // ---------- Cancellation job ----------

        private static (SubscriptionCancellationJob job, Mock<IPaymentGateway> gateway, Mock<IUnitOfWork> unitOfWork, FakeBackgroundJobService jobs) CancellationJob ( Subscription subscription )
        {
            var read = new Mock<ISubscriptionsReadOnlyRepository>();
            read.Setup(r => r.GetByIdAsync(subscription.Id , It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

            var gateway = new Mock<IPaymentGateway>();
            var unitOfWork = new Mock<IUnitOfWork>();
            var jobs = new FakeBackgroundJobService();

            var job = new SubscriptionCancellationJob(
                read.Object ,
                new Mock<ISubscriptionsUpdateOnlyRepository>().Object ,
                unitOfWork.Object ,
                gateway.Object ,
                jobs ,
                new LoggerServiceBuilder<SubscriptionCancellationJob>().Build());

            return (job, gateway, unitOfWork, jobs);
        }

        [Fact]
        public async Task Job_Stops_Charges_At_PagarMe_And_Keeps_Premium_Until_The_Paid_Period_Ends ()
        {
            var paidUntil = DateTime.UtcNow.AddHours(6);
            var subscription = new Subscription
            {
                Id = 10 , Status = SubscriptionStatus.Active , CancelAtPeriodEnd = true ,
                PagarmeSubscriptionId = "sub_1" , CancellationJobId = "job-1" , NextBillingDate = paidUntil
            };
            var (job, gateway, unitOfWork, jobs) = CancellationJob(subscription);

            await job.ExecuteAsync(10 , CancellationToken.None);

            gateway.Verify(g => g.CancelSubscriptionAsync("sub_1" , It.IsAny<CancellationToken>()) , Times.Once);
            subscription.Status.Should().Be(SubscriptionStatus.Active);
            subscription.EndsAt.Should().Be(paidUntil);
            jobs.ScheduledExpirations.Should().ContainSingle().Which.Should().Be((10L, paidUntil));
            subscription.CancellationJobId.Should().Be("expire-10");
            unitOfWork.Verify(u => u.Commit(It.IsAny<CancellationToken>()) , Times.Once);
        }

        [Fact]
        public async Task Reactivation_Is_Refused_Once_Charges_Were_Stopped_At_PagarMe ()
        {
            var subscription = new Subscription
            {
                Status = SubscriptionStatus.Active , CancelAtPeriodEnd = true , EndsAt = DateTime.UtcNow.AddHours(3)
            };
            var read = new Mock<ISubscriptionsReadOnlyRepository>();
            read.Setup(r => r.GetActiveByUserIdAsync(1 , It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

            var useCase = new MiCash.Application.UseCases.Subscriptions.Reactivate.ReactivateSubscriptionUseCase(
                LoggedUserBuilder.Build(new User { Id = 1 }) ,
                read.Object ,
                new Mock<ISubscriptionsUpdateOnlyRepository>().Object ,
                new FakeBackgroundJobService() ,
                new Mock<IUnitOfWork>().Object);

            var act = () => useCase.ExecuteAsync(CancellationToken.None);

            (await act.Should().ThrowAsync<BadRequestException>())
                .Which.GetErrors().Should().Contain(ResourceErrorMessages.SUBSCRIPTION_CANCELLATION_ALREADY_PROCESSED);
            subscription.CancelAtPeriodEnd.Should().BeTrue();
        }

        // ---------- Expiration job ----------

        private static async Task<Subscription> RunExpiration ( DateTime? endsAt , DateTimeOffset now )
        {
            var subscription = new Subscription { Id = 10 , Status = SubscriptionStatus.Active , CancelAtPeriodEnd = true , EndsAt = endsAt };
            var read = new Mock<ISubscriptionsReadOnlyRepository>();
            read.Setup(r => r.GetByIdAsync(10 , It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

            var job = new SubscriptionExpirationJob(
                read.Object ,
                new Mock<ISubscriptionsUpdateOnlyRepository>().Object ,
                new Mock<IUnitOfWork>().Object ,
                new MutableTimeProvider(now) ,
                new LoggerServiceBuilder<SubscriptionExpirationJob>().Build());

            await job.ExecuteAsync(10 , CancellationToken.None);

            return subscription;
        }

        [Fact]
        public async Task Expiration_At_The_End_Of_The_Paid_Period_Returns_The_User_To_Free ()
        {
            var subscription = await RunExpiration(NextBilling , new DateTimeOffset(NextBilling));

            subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
            subscription.EndsAt.Should().Be(NextBilling);
            subscription.CancelAtPeriodEnd.Should().BeFalse();
        }

        [Fact]
        public async Task Expiration_Never_Ends_Access_Before_The_Paid_Period ()
        {
            var subscription = await RunExpiration(NextBilling , new DateTimeOffset(NextBilling.AddMinutes(-1)));

            subscription.Status.Should().Be(SubscriptionStatus.Active);
        }

        [Fact]
        public async Task Job_Failing_At_PagarMe_Keeps_The_Subscription_And_Throws_For_Hangfire_Retry ()
        {
            var subscription = new Subscription
            {
                Id = 10 , Status = SubscriptionStatus.Active , CancelAtPeriodEnd = true , PagarmeSubscriptionId = "sub_1"
            };
            var (job, gateway, unitOfWork, _) = CancellationJob(subscription);
            gateway.Setup(g => g.CancelSubscriptionAsync(It.IsAny<string>() , It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("down"));

            var act = () => job.ExecuteAsync(10 , CancellationToken.None);

            await act.Should().ThrowAsync<HttpRequestException>();
            subscription.Status.Should().Be(SubscriptionStatus.Active);
            unitOfWork.Verify(u => u.Commit(It.IsAny<CancellationToken>()) , Times.Never);
        }

        [Fact]
        public async Task Job_Also_Cancels_When_The_Last_Charge_Failed ()
        {
            // Pagar.me keeps retrying a failed charge: the user asked to cancel, so the job must still run.
            var subscription = new Subscription
            {
                Id = 10 , Status = SubscriptionStatus.PaymentFailed , CancelAtPeriodEnd = true , PagarmeSubscriptionId = "sub_1"
            };
            var (job, gateway, _, _) = CancellationJob(subscription);

            await job.ExecuteAsync(10 , CancellationToken.None);

            gateway.Verify(g => g.CancelSubscriptionAsync("sub_1" , It.IsAny<CancellationToken>()) , Times.Once);
            // An unpaid cycle is not a "paid period" to keep: it ends right away.
            subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        }

        // ---------- Reconciliation job ----------

        private static (SubscriptionReconciliationJob job, Mock<ISubscriptionsUpdateOnlyRepository> update) Reconciliation (
            Subscription? subscription , GatewaySubscription? remote , Subscription? ended = null )
        {
            var read = new Mock<ISubscriptionsReadOnlyRepository>();
            read.Setup(r => r.GetActiveWithBillingDateBeforeAsync(It.IsAny<DateTime>() , It.IsAny<CancellationToken>()))
                .ReturnsAsync(subscription is null ? [] : [subscription]);
            read.Setup(r => r.GetActiveEndedBeforeAsync(It.IsAny<DateTime>() , It.IsAny<CancellationToken>()))
                .ReturnsAsync(ended is null ? [] : [ended]);

            var gateway = new Mock<IPaymentGateway>();
            gateway.Setup(g => g.GetSubscriptionAsync("sub_1" , It.IsAny<CancellationToken>())).ReturnsAsync(remote);

            var update = new Mock<ISubscriptionsUpdateOnlyRepository>();

            var job = new SubscriptionReconciliationJob(
                read.Object ,
                update.Object ,
                new Mock<IUnitOfWork>().Object ,
                gateway.Object ,
                new FakeBackgroundJobService() ,
                new MutableTimeProvider(new DateTimeOffset(2026 , 12 , 10 , 6 , 0 , 0 , TimeSpan.Zero)) ,
                new LoggerServiceBuilder<SubscriptionReconciliationJob>().Build());

            return (job, update);
        }

        private static Subscription Overdue () => new()
        {
            Id = 10 , Status = SubscriptionStatus.Active , PagarmeSubscriptionId = "sub_1" , NextBillingDate = NextBilling
        };

        [Fact]
        public async Task Reconciliation_Ends_A_Paid_Period_Whose_Expiration_Job_Was_Lost ()
        {
            var ended = new Subscription { Id = 11 , Status = SubscriptionStatus.Active , CancelAtPeriodEnd = true , EndsAt = NextBilling };
            var (job, update) = Reconciliation(subscription: null , remote: null , ended);

            await job.ExecuteAsync(CancellationToken.None);

            ended.Status.Should().Be(SubscriptionStatus.Cancelled);
            ended.EndsAt.Should().Be(NextBilling);
            update.Verify(u => u.Update(ended , It.IsAny<CancellationToken>()) , Times.Once);
        }

        [Fact]
        public async Task Reconciliation_Applies_A_Lost_Renewal ()
        {
            var subscription = Overdue();
            var (job, update) = Reconciliation(subscription ,
                new GatewaySubscription { Status = "active" , NextBillingAt = NextBilling.AddMonths(1) });

            await job.ExecuteAsync(CancellationToken.None);

            subscription.NextBillingDate.Should().Be(NextBilling.AddMonths(1));
            subscription.Status.Should().Be(SubscriptionStatus.Active);
            update.Verify(u => u.Update(subscription , It.IsAny<CancellationToken>()) , Times.Once);
        }

        [Fact]
        public async Task Reconciliation_Applies_A_Lost_Cancellation ()
        {
            var canceledAt = new DateTime(2026 , 12 , 1 , 0 , 0 , 0 , DateTimeKind.Utc);
            var subscription = Overdue();
            var (job, _) = Reconciliation(subscription ,
                new GatewaySubscription { Status = "canceled" , CanceledAt = canceledAt });

            await job.ExecuteAsync(CancellationToken.None);

            subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
            subscription.EndsAt.Should().Be(canceledAt);
        }

        [Fact]
        public async Task Reconciliation_Leaves_A_Renewal_Still_Pending_At_PagarMe ()
        {
            var subscription = Overdue();
            var (job, update) = Reconciliation(subscription ,
                new GatewaySubscription { Status = "active" , NextBillingAt = NextBilling });

            await job.ExecuteAsync(CancellationToken.None);

            subscription.Status.Should().Be(SubscriptionStatus.Active);
            update.Verify(u => u.Update(It.IsAny<Subscription>() , It.IsAny<CancellationToken>()) , Times.Never);
        }

        // ---------- Checkout ----------

        private static (CreateCheckoutUseCase useCase, Mock<IPaymentGateway> gateway) Checkout ( Plan? plan , Subscription? current )
        {
            var plans = new Mock<IPlansReadOnlyRepository>();
            plans.Setup(p => p.GetByIdAsync(It.IsAny<long>() , It.IsAny<CancellationToken>())).ReturnsAsync(plan);

            var subscriptions = new Mock<ISubscriptionsReadOnlyRepository>();
            subscriptions.Setup(s => s.GetCurrentByUserIdAsync(1 , It.IsAny<CancellationToken>())).ReturnsAsync(current);

            var gateway = new Mock<IPaymentGateway>();
            gateway.Setup(g => g.CreateSubscriptionCheckoutAsync(It.IsAny<User>() , It.IsAny<Plan>() , It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateCheckoutResult { CheckoutUrl = "https://pay" , PaymentLinkId = "pl_1" , OrderCode = "o" });

            var useCase = new CreateCheckoutUseCase(
                LoggedUserBuilder.Build(new User { Id = 1 }) ,
                plans.Object ,
                gateway.Object ,
                subscriptions.Object ,
                new Mock<IPaymentLinkRepository>().Object ,
                new Mock<IUnitOfWork>().Object);

            return (useCase, gateway);
        }

        private static Plan Premium ( bool isActive = true ) => new() { Id = 2 , Name = "Premium" , IsActive = isActive , PagarmePlanId = "plan_x" };

        [Fact]
        public async Task Checkout_Of_An_Inactive_Plan_Is_Not_Found ()
        {
            var (useCase, gateway) = Checkout(Premium(isActive: false) , current: null);

            var act = () => useCase.ExecuteAsync(new RequestCreateCheckoutJson { PlanId = 2 } , CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
            gateway.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Checkout_Is_Blocked_While_A_Recent_Payment_Is_Being_Confirmed ()
        {
            var pending = new Subscription { Status = SubscriptionStatus.Pending , CreatedAt = DateTime.UtcNow.AddMinutes(-5) };
            var (useCase, _) = Checkout(Premium() , pending);

            var act = () => useCase.ExecuteAsync(new RequestCreateCheckoutJson { PlanId = 2 } , CancellationToken.None);

            (await act.Should().ThrowAsync<ErrorOnValidationException>())
                .Which.GetErrors().Should().Contain(ResourceErrorMessages.SUBSCRIPTION_PAYMENT_IN_PROGRESS);
        }

        [Fact]
        public async Task Checkout_Is_Allowed_Again_After_A_Stale_Pending_Subscription ()
        {
            var stale = new Subscription { Status = SubscriptionStatus.Pending , CreatedAt = DateTime.UtcNow.AddDays(-2) };
            var (useCase, _) = Checkout(Premium() , stale);

            var response = await useCase.ExecuteAsync(new RequestCreateCheckoutJson { PlanId = 2 } , CancellationToken.None);

            response.CheckoutUrl.Should().Be("https://pay");
        }
    }
}
