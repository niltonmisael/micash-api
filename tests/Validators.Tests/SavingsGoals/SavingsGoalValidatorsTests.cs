using FluentAssertions;
using MiCash.Application.UseCases.SavingsGoals;
using MiCash.Communication.Requests.SavingsGoals;
using MiCash.Exception;

namespace Validators.Tests.SavingsGoals
{
    public class SavingsGoalValidatorsTests
    {
        private static readonly DateOnly Today = new(2026 , 10 , 15);

        private static RequestRegisterSavingsGoalJson ValidRegister () => new()
        {
            Name = "Viagem" ,
            TargetAmount = 5000 ,
            Deadline = new DateOnly(2026 , 12 , 31) ,
            InitialAmount = 3200 ,
            Icon = "plane" ,
            Color = "#F2803A"
        };

        [Fact]
        public void Register_Success ()
        {
            new ValidatorRegisterSavingsGoal(Today).Validate(ValidRegister()).IsValid.Should().BeTrue();
        }

        [Fact]
        public void Register_Success_Without_Optional_Fields ()
        {
            var request = ValidRegister();
            request.Deadline = null;
            request.InitialAmount = null;

            new ValidatorRegisterSavingsGoal(Today).Validate(request).IsValid.Should().BeTrue();
        }

        [Fact]
        public void Register_Deadline_In_Current_Month_Is_Valid ()
        {
            var request = ValidRegister();
            request.Deadline = new DateOnly(2026 , 10 , 1);

            new ValidatorRegisterSavingsGoal(Today).Validate(request).IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("" , nameof(ResourceErrorMessages.NAME_EMPTY))]
        [InlineData("   " , nameof(ResourceErrorMessages.NAME_EMPTY))]
        public void Register_Name_Empty ( string name , string _ )
        {
            var request = ValidRegister();
            request.Name = name;

            AssertSingleError(new ValidatorRegisterSavingsGoal(Today).Validate(request) , ResourceErrorMessages.NAME_EMPTY);
        }

        [Fact]
        public void Register_Name_Too_Long ()
        {
            var request = ValidRegister();
            request.Name = new string('a' , 101);

            AssertSingleError(new ValidatorRegisterSavingsGoal(Today).Validate(request) , ResourceErrorMessages.SAVINGS_GOAL_NAME_TOO_LONG);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(10.555)]
        public void Register_Target_Amount_Invalid ( decimal amount )
        {
            var request = ValidRegister();
            request.TargetAmount = amount;

            AssertSingleError(new ValidatorRegisterSavingsGoal(Today).Validate(request) , ResourceErrorMessages.SAVINGS_GOAL_TARGET_AMOUNT_INVALID);
        }

        [Fact]
        public void Register_Deadline_In_Past_Month ()
        {
            var request = ValidRegister();
            request.Deadline = new DateOnly(2026 , 9 , 30);

            AssertSingleError(new ValidatorRegisterSavingsGoal(Today).Validate(request) , ResourceErrorMessages.SAVINGS_GOAL_DEADLINE_IN_PAST);
        }

        [Fact]
        public void Register_Initial_Amount_Negative ()
        {
            var request = ValidRegister();
            request.InitialAmount = -10;

            AssertSingleError(new ValidatorRegisterSavingsGoal(Today).Validate(request) , ResourceErrorMessages.SAVINGS_GOAL_INITIAL_AMOUNT_INVALID);
        }

        [Fact]
        public void Register_Icon_And_Color_Required ()
        {
            var request = ValidRegister();
            request.Icon = "";
            request.Color = "";

            var result = new ValidatorRegisterSavingsGoal(Today).Validate(request);

            result.Errors.Select(e => e.ErrorMessage).Should()
                .BeEquivalentTo([ResourceErrorMessages.ICON_REQUIRED , ResourceErrorMessages.COLOR_REQUIRED]);
        }

        [Fact]
        public void Update_Has_No_Deadline_Rule ()
        {
            // The use case only checks the deadline when it changes (expired goals stay editable).
            var request = new RequestUpdateSavingsGoalJson
            {
                Name = "Viagem" ,
                TargetAmount = 5000 ,
                Deadline = new DateOnly(2020 , 1 , 31) ,
                Icon = "plane" ,
                Color = "#F2803A"
            };

            new ValidatorUpdateSavingsGoal().Validate(request).IsValid.Should().BeTrue();
        }

        [Fact]
        public void Movement_Success_Today ()
        {
            var request = new RequestSavingsMovementJson { Amount = 500 , Date = Today };

            new ValidatorSavingsMovement(Today).Validate(request).IsValid.Should().BeTrue();
        }

        [Fact]
        public void Movement_Date_In_Future ()
        {
            var request = new RequestSavingsMovementJson { Amount = 500 , Date = Today.AddDays(1) };

            AssertSingleError(new ValidatorSavingsMovement(Today).Validate(request) , ResourceErrorMessages.SAVINGS_MOVEMENT_DATE_IN_FUTURE);
        }

        [Fact]
        public void Movement_Date_Missing ()
        {
            var request = new RequestSavingsMovementJson { Amount = 500 };

            AssertSingleError(new ValidatorSavingsMovement(Today).Validate(request) , ResourceErrorMessages.SAVINGS_MOVEMENT_DATE_REQUIRED);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        [InlineData(1.001)]
        public void Movement_Amount_Invalid ( decimal amount )
        {
            var request = new RequestSavingsMovementJson { Amount = amount , Date = Today };

            AssertSingleError(new ValidatorSavingsMovement(Today).Validate(request) , ResourceErrorMessages.SAVINGS_MOVEMENT_AMOUNT_INVALID);
        }

        private static void AssertSingleError ( FluentValidation.Results.ValidationResult result , string message )
        {
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be(message);
        }
    }
}
