using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Plans;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Time;
using FluentAssertions;
using MiCash.Application.UseCases.FinancialChat;
using MiCash.Application.UseCases.SavingsGoals.Register;
using MiCash.Communication.Requests.SavingsGoals;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.Conversations;
using MiCash.Domain.Repositories.SavingsGoals;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Services.Clock;
using MiCash.Domain.Services.Plans;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using Moq;

namespace UseCases.Test.Plans
{
    public class PlanLimitsTest
    {
        [Theory]
        [InlineData(2026 , 11 , true)]   // current month
        [InlineData(2026 , 8 , true)]    // 3 months back
        [InlineData(2026 , 7 , false)]   // 4 months back
        [InlineData(2025 , 12 , false)]
        [InlineData(2027 , 1 , true)]    // future months are planning, always allowed
        public void Free_History_Is_The_Current_Month_And_The_3_Previous ( int year , int month , bool allowed )
        {
            var today = new DateOnly(2026 , 11 , 3);

            PlanLimits.Free.AllowsMonth(today , year , month).Should().Be(allowed);
            PlanLimits.Premium.AllowsMonth(today , year , month).Should().BeTrue();
        }

        [Fact]
        public void Free_History_Crosses_The_Year ()
        {
            var today = new DateOnly(2027 , 2 , 1);

            PlanLimits.Free.AllowsMonth(today , 2026 , 11).Should().BeTrue();
            PlanLimits.Free.AllowsMonth(today , 2026 , 10).Should().BeFalse();
        }

        private static readonly DateTime Now = new(2026 , 11 , 10 , 12 , 0 , 0 , DateTimeKind.Utc);

        public static TheoryData<string , Subscription? , bool> AccessCases => new()
        {
            { "no subscription" , null , false },
            { "active" , new Subscription { UserId = 1 , Status = SubscriptionStatus.Active } , true },
            {
                "canceled, still inside the paid period" ,
                new Subscription { UserId = 1 , Status = SubscriptionStatus.Active , CancelAtPeriodEnd = true , EndsAt = Now.AddDays(20) } ,
                true
            },
            { "pending checkout" , new Subscription { UserId = 1 , Status = SubscriptionStatus.Pending } , false },
            {
                "renewal refused, inside the 3-day grace" ,
                new Subscription { UserId = 1 , Status = SubscriptionStatus.PaymentFailed , PaymentGraceUntil = Now.AddHours(1) } ,
                true
            },
            {
                "renewal refused, grace over" ,
                new Subscription { UserId = 1 , Status = SubscriptionStatus.PaymentFailed , PaymentGraceUntil = Now.AddSeconds(-1) } ,
                false
            },
            { "first charge refused (no grace)" , new Subscription { UserId = 1 , Status = SubscriptionStatus.PaymentFailed } , false },
            { "cancelled / expired" , new Subscription { UserId = 1 , Status = SubscriptionStatus.Cancelled } , false },
        };

        [Theory]
        [MemberData(nameof(AccessCases))]
        public async Task Premium_Comes_Only_From_The_Server_Subscription_Rule ( string scenario , Subscription? subscription , bool premium )
        {
            var repository = new Mock<ISubscriptionsReadOnlyRepository>();
            repository.Setup(r => r.GetWithAccessByUserIdAsync(1 , It.IsAny<CancellationToken>()))
                .ReturnsAsync(subscription);

            var service = new UserPlanService(repository.Object , new MutableTimeProvider(new DateTimeOffset(Now)));

            var limits = await service.GetLimitsAsync(new User { Id = 1 } , CancellationToken.None);
            await service.GetLimitsAsync(new User { Id = 1 } , CancellationToken.None);

            limits.IsPremium.Should().Be(premium , scenario);
            SubscriptionAccess.HasPremium(subscription , Now).Should().Be(premium , scenario);
            repository.Verify(r => r.GetWithAccessByUserIdAsync(1 , It.IsAny<CancellationToken>()) , Times.Once , "read once per request");
        }

        [Fact]
        public void Brasilia_Day_Starts_At_03_00_Utc ()
        {
            var clock = new BrazilClock(new MutableTimeProvider(new DateTimeOffset(2026 , 11 , 3 , 2 , 0 , 0 , TimeSpan.Zero)));

            clock.Today.Should().Be(new DateOnly(2026 , 11 , 2) , "02:00 UTC is still 23:00 of the previous day in Brasília");
            clock.StartOfDayUtc(new DateOnly(2026 , 11 , 3)).Should().Be(new DateTime(2026 , 11 , 3 , 3 , 0 , 0 , DateTimeKind.Utc));
        }

        [Fact]
        public void Brasilia_Month_Range_In_Utc ()
        {
            var (start, end) = BrazilClock.MonthRangeUtc(2026 , 12);

            start.Should().Be(new DateTime(2026 , 12 , 1 , 3 , 0 , 0 , DateTimeKind.Utc));
            end.Should().Be(new DateTime(2027 , 1 , 1 , 3 , 0 , 0 , DateTimeKind.Utc) , "December ends at midnight of January 1st in Brasília");
        }

        [Theory]
        [InlineData(DateTimeKind.Utc)]
        [InlineData(DateTimeKind.Unspecified)]
        public void Utc_Instant_As_Brasilia_Wall_Time ( DateTimeKind kind )
        {
            // 01:30 UTC on Dec 1st = 22:30 on Nov 30th in Brasília.
            BrazilClock.ToBrasilia(new DateTime(2026 , 12 , 1 , 1 , 30 , 0 , kind))
                .Should().Be(new DateTime(2026 , 11 , 30 , 22 , 30 , 0));
        }
    }

    public class ChatUsageLimiterTest
    {
        private static readonly User USER = new() { Id = 1 };

        // 2026-11-10 15:00 UTC = 12:00 in Brasília.
        private static readonly DateTimeOffset NOW = new(2026 , 11 , 10 , 15 , 0 , 0 , TimeSpan.Zero);

        [Theory]
        [InlineData(4 , false)]
        [InlineData(5 , true)]
        public async Task Free_Has_5_Messages_Per_Month ( int usedThisMonth , bool blocked )
        {
            var messages = Messages(month: usedThisMonth);

            var act = () => Limiter(PlanLimits.Free , messages).EnsureCanSendAsync(USER , CancellationToken.None);

            if ( blocked ) {
                var error = (await act.Should().ThrowAsync<PlanLimitException>()).Which;
                error.StatusCode.Should().Be(403);
                error.ErrorCode.Should().Be(PlanLimitException.CODE);
                error.RetryAt.Should().Be(new DateTime(2026 , 12 , 1 , 3 , 0 , 0 , DateTimeKind.Utc) , "next month starts at 00:00 in Brasília");
            } else {
                await act.Should().NotThrowAsync();
            }
        }

        [Fact]
        public async Task Premium_Has_30_Messages_Per_Day ()
        {
            var act = () => Limiter(PlanLimits.Premium , Messages(day: 30)).EnsureCanSendAsync(USER , CancellationToken.None);

            var error = (await act.Should().ThrowAsync<ChatRateLimitException>()).Which;
            error.StatusCode.Should().Be(429);
            error.ErrorCode.Should().Be(ChatRateLimitException.CODE);
            error.RetryAt.Should().Be(new DateTime(2026 , 11 , 11 , 3 , 0 , 0 , DateTimeKind.Utc));
            error.GetErrors().Should().ContainSingle().Which.Should().Contain("30");
        }

        [Fact]
        public async Task Premium_Has_5_Messages_Per_Minute ()
        {
            var act = () => Limiter(PlanLimits.Premium , Messages(day: 5 , minute: 5)).EnsureCanSendAsync(USER , CancellationToken.None);

            (await act.Should().ThrowAsync<ChatRateLimitException>())
                .Which.GetErrors().Should().ContainSingle().Which.Should().Be(ResourceErrorMessages.CHAT_MINUTE_LIMIT);
        }

        [Fact]
        public async Task Premium_Under_The_Limits_Can_Send ()
        {
            await Limiter(PlanLimits.Premium , Messages(day: 29 , minute: 4)).Invoking(l => l.EnsureCanSendAsync(USER , CancellationToken.None))
                .Should().NotThrowAsync();
        }

        [Fact]
        public async Task Usage_Reports_The_Plan_Period ()
        {
            var free = await Limiter(PlanLimits.Free , Messages(month: 2)).GetUsageAsync(USER , CancellationToken.None);
            var premium = await Limiter(PlanLimits.Premium , Messages(day: 7)).GetUsageAsync(USER , CancellationToken.None);

            free.Should().BeEquivalentTo(new { Period = "month" , Limit = 5 , Used = 2 , Remaining = 3 });
            premium.Should().BeEquivalentTo(new { Period = "day" , Limit = 30 , Used = 7 , Remaining = 23 });
        }

        /// <summary>Counts by window start: month start, day start (Brasília) and one minute ago.</summary>
        private static Mock<IConversationMessageReadOnlyRepository> Messages ( int month = 0 , int day = 0 , int minute = 0 )
        {
            var monthStart = new DateTime(2026 , 11 , 1 , 3 , 0 , 0 , DateTimeKind.Utc);
            var dayStart = new DateTime(2026 , 11 , 10 , 3 , 0 , 0 , DateTimeKind.Utc);

            var repository = new Mock<IConversationMessageReadOnlyRepository>();
            repository.Setup(r => r.CountUserMessagesSinceAsync(USER.Id , monthStart , It.IsAny<CancellationToken>())).ReturnsAsync(month);
            repository.Setup(r => r.CountUserMessagesSinceAsync(USER.Id , dayStart , It.IsAny<CancellationToken>())).ReturnsAsync(day);
            repository.Setup(r => r.CountUserMessagesSinceAsync(USER.Id , NOW.UtcDateTime.AddMinutes(-1) , It.IsAny<CancellationToken>())).ReturnsAsync(minute);

            return repository;
        }

        private static ChatUsageLimiter Limiter ( PlanLimits plan , Mock<IConversationMessageReadOnlyRepository> messages ) =>
            new(UserPlanServiceBuilder.Build(plan) , messages.Object , new BrazilClock(new MutableTimeProvider(NOW)));
    }

    public class RegisterSavingsGoalPlanTest
    {
        [Theory]
        [InlineData(0 , false)]
        [InlineData(1 , true)]
        public async Task Free_Plan_Allows_1_Savings_Goal ( int existing , bool blocked )
        {
            var user = new User { Id = 1 };

            var readRepository = new Mock<ISavingsGoalReadOnlyRepository>();
            readRepository.Setup(r => r.GetAllWithBalanceAsync(user.Id , It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Range(1 , existing).Select(i => new SavingsGoalWithBalance(new SavingsGoal { Id = i } , 0m)).ToList());

            var writeRepository = new Mock<ISavingsGoalWriteOnlyRepository>();

            var useCase = new RegisterSavingsGoalUseCase(
                writeRepository.Object ,
                readRepository.Object ,
                LoggedUserBuilder.Build(user) ,
                UnitOfWorkBuilder.Build() ,
                new BrazilClock(new MutableTimeProvider(new DateTimeOffset(2026 , 11 , 10 , 15 , 0 , 0 , TimeSpan.Zero))) ,
                UserPlanServiceBuilder.Free());

            var act = () => useCase.Execute(
                new RequestRegisterSavingsGoalJson { Name = "Viagem" , TargetAmount = 1000m , Icon = "plane" , Color = "#F58A3C" } ,
                CancellationToken.None);

            if ( blocked ) {
                (await act.Should().ThrowAsync<PlanLimitException>()).Which.ErrorCode.Should().Be(PlanLimitException.CODE);
                writeRepository.Verify(r => r.AddAsync(It.IsAny<SavingsGoal>() , It.IsAny<CancellationToken>()) , Times.Never);
            } else {
                await act.Should().NotThrowAsync();
            }
        }
    }
}
