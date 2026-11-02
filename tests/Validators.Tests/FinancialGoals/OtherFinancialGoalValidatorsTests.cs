using CommonTestUtilities.Requests;
using FluentAssertions;
using MiCash.Application.UseCases.FinancialGoals;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace Validators.Tests.FinancialGoals
{
    public class UpdateFinancialGoalValidatorTests
    {
        [Fact]
        public void Success ()
        {
            var result = new ValidatorUpdateFinancialGoal().Validate(RequestFinancialGoalJsonBuilder.BuildUpdate());

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Error_Target_Amount_Zero ()
        {
            var request = RequestFinancialGoalJsonBuilder.BuildUpdate();
            request.TargetAmount = 0;

            var result = new ValidatorUpdateFinancialGoal().Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle()
                .And.Contain(e => e.ErrorMessage == ResourceErrorMessages.TARGET_AMOUNT_MUST_BE_GREATER_THAN_ZERO);
        }
    }

    public class CopyFinancialGoalsValidatorTests
    {
        [Fact]
        public void Success ()
        {
            var result = new ValidatorCopyFinancialGoals().Validate(RequestFinancialGoalJsonBuilder.BuildCopy());

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Error_Same_Period ()
        {
            var request = RequestFinancialGoalJsonBuilder.BuildCopy();
            request.TargetMonth = request.SourceMonth;
            request.TargetYear = request.SourceYear;

            var result = new ValidatorCopyFinancialGoals().Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle()
                .And.Contain(e => e.ErrorMessage == ResourceErrorMessages.FINANCIAL_GOAL_SAME_PERIOD);
        }

        [Fact]
        public void Error_Invalid_Months ()
        {
            var request = RequestFinancialGoalJsonBuilder.BuildCopy();
            request.SourceMonth = 0;
            request.TargetMonth = 13;

            var result = new ValidatorCopyFinancialGoals().Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Where(e => e.ErrorMessage == ResourceErrorMessages.MONTH_INVALID_RANGE).Should().HaveCount(2);
        }

        [Fact]
        public void Error_Target_Year_In_The_Past ()
        {
            var request = RequestFinancialGoalJsonBuilder.BuildCopy();
            request.TargetYear = DateTime.UtcNow.Year - 1;

            var result = new ValidatorCopyFinancialGoals().Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == ResourceErrorMessages.YEAR_LESS_THAN_CURRENT);
        }
    }

    public class FinancialGoalPeriodValidatorTests
    {
        [Fact]
        public void Success ()
        {
            var act = () => FinancialGoalPeriodValidator.Validate(6 , DateTime.UtcNow.Year);

            act.Should().NotThrow();
        }

        [Theory]
        [InlineData(0 , 2026 , 1)]
        [InlineData(13 , 2026 , 1)]
        [InlineData(5 , 0 , 1)]
        [InlineData(0 , 1 , 2)]
        public void Error_Invalid_Period ( int month , int year , int expectedErrors )
        {
            var act = () => FinancialGoalPeriodValidator.Validate(month , year);

            act.Should().Throw<ErrorOnValidationException>()
                .Which.GetErrors().Should().HaveCount(expectedErrors);
        }
    }
}
