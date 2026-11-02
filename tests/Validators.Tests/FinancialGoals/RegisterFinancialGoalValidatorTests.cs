using CommonTestUtilities.Requests;
using FluentAssertions;
using MiCash.Application.UseCases.FinancialGoals;
using MiCash.Exception;

namespace Validators.Tests.FinancialGoals
{
    public class RegisterFinancialGoalValidatorTests
    {
        [Fact]
        public void Success ()
        {
            var request = RequestFinancialGoalJsonBuilder.BuildRegister();

            var result = new ValidatorFinancialGoal().Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void Error_Target_Amount_Invalid ( decimal amount )
        {
            var request = RequestFinancialGoalJsonBuilder.BuildRegister();
            request.TargetAmount = amount;

            var result = new ValidatorFinancialGoal().Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle()
                .And.Contain(e => e.ErrorMessage == ResourceErrorMessages.TARGET_AMOUNT_MUST_BE_GREATER_THAN_ZERO);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        public void Error_Month_Invalid ( int month )
        {
            var request = RequestFinancialGoalJsonBuilder.BuildRegister();
            request.Month = month;

            var result = new ValidatorFinancialGoal().Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle()
                .And.Contain(e => e.ErrorMessage == ResourceErrorMessages.MONTH_INVALID_RANGE);
        }

        [Fact]
        public void Error_Year_Less_Than_Current ()
        {
            var request = RequestFinancialGoalJsonBuilder.BuildRegister();
            request.Year = DateTime.UtcNow.Year - 1;

            var result = new ValidatorFinancialGoal().Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle()
                .And.Contain(e => e.ErrorMessage == ResourceErrorMessages.YEAR_LESS_THAN_CURRENT);
        }

        [Fact]
        public void Error_Category_Invalid ()
        {
            var request = RequestFinancialGoalJsonBuilder.BuildRegister(categoryId: 0);

            var result = new ValidatorFinancialGoal().Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle()
                .And.Contain(e => e.ErrorMessage == ResourceErrorMessages.CATEGORY_NOT_FOUND);
        }
    }
}
