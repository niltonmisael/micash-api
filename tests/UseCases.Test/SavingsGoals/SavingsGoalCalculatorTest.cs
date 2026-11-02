using FluentAssertions;
using MiCash.Domain.Services.Clock;
using MiCash.Domain.Services.SavingsGoals;

namespace UseCases.Test.SavingsGoals
{
    public class SavingsGoalCalculatorTest
    {
        private static readonly DateOnly Today = new(2026 , 10 , 15);

        [Fact]
        public void Prototype_29_Example ()
        {
            // Out/2026, deadline dez/2026: R$ 1.800 left in 3 months => R$ 600.
            var result = SavingsGoalCalculator.Calculate(5000 , 3200 , new DateOnly(2026 , 12 , 31) , Today);

            result.SavedAmount.Should().Be(3200);
            result.RemainingAmount.Should().Be(1800);
            result.Percentage.Should().Be(64);
            result.IsCompleted.Should().BeFalse();
            result.MonthlyAmountNeeded.Should().Be(600);
        }

        [Fact]
        public void Monthly_Amount_Is_Rounded_Up_To_Reach_The_Target ()
        {
            var result = SavingsGoalCalculator.Calculate(1000 , 0 , new DateOnly(2026 , 12 , 31) , Today);

            result.MonthlyAmountNeeded.Should().Be(333.34m);
        }

        [Fact]
        public void Deadline_In_Current_Month_Asks_Everything_Now ()
        {
            var result = SavingsGoalCalculator.Calculate(1000 , 400 , new DateOnly(2026 , 10 , 31) , Today);

            result.MonthlyAmountNeeded.Should().Be(600);
        }

        [Fact]
        public void Overdue_Deadline_Asks_Everything_Now ()
        {
            var result = SavingsGoalCalculator.Calculate(1000 , 400 , new DateOnly(2026 , 3 , 31) , Today);

            result.MonthlyAmountNeeded.Should().Be(600);
        }

        [Fact]
        public void No_Deadline_Has_No_Monthly_Amount ()
        {
            var result = SavingsGoalCalculator.Calculate(1000 , 400 , null , Today);

            result.MonthlyAmountNeeded.Should().BeNull();
        }

        [Fact]
        public void Completed_Goal_Can_Exceed_100_Percent ()
        {
            var result = SavingsGoalCalculator.Calculate(1000 , 1250 , new DateOnly(2026 , 12 , 31) , Today);

            result.IsCompleted.Should().BeTrue();
            result.Percentage.Should().Be(125);
            result.RemainingAmount.Should().Be(0);
            result.MonthlyAmountNeeded.Should().BeNull();
        }

        [Theory]
        // 23:30 in Brasília on Oct 31 is already Nov 1 in UTC: the Brazilian day must still be Oct 31.
        [InlineData("2026-11-01T02:30:00Z" , "2026-10-31")]
        [InlineData("2026-11-01T03:30:00Z" , "2026-11-01")]
        public void Brazil_Clock_Uses_Brasilia_Day ( string utcNow , string expectedDay )
        {
            var clock = new BrazilClock(new FixedTimeProvider(DateTimeOffset.Parse(utcNow)));

            clock.Today.Should().Be(DateOnly.Parse(expectedDay));
        }

        private sealed class FixedTimeProvider ( DateTimeOffset now ) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow () => now;
        }
    }
}
