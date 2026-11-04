using CommonTestUtilities.Plans;
using MiCash.Domain.Services.Plans;
using CommonTestUtilities.Cache;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Mapper;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using FluentAssertions;
using MiCash.Application.UseCases.FinancialGoals.Register;
using MiCash.Domain.Entities;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace UseCases.Test.FinancialGoals
{
    public class RegisterFinancialGoalUseCaseTest
    {
        [Fact]
        public async Task Success ()
        {
            var user = UserBuilder.Build();
            var category = CategoryBuilder.Build(user);
            var request = RequestFinancialGoalJsonBuilder.BuildRegister(category.Id);

            var useCase = CreateUseCase(user , category);

            var act = async () => await useCase.Execute(request , CancellationToken.None);

            await act.Should().NotThrowAsync();
        }

        [Theory]
        [InlineData(2 , false)]
        [InlineData(3 , true)]
        public async Task Free_Plan_Allows_Up_To_3_Goals_Per_Month ( int existing , bool blocked )
        {
            var user = UserBuilder.Build();
            var category = CategoryBuilder.Build(user);
            var request = RequestFinancialGoalJsonBuilder.BuildRegister(category.Id);

            var goalRepository = new FinancialGoalReadOnlyRepositoryBuilder()
                .GetByPeriod(user , request.Month , request.Year , FinancialGoalBuilder.Collection(user , (uint) existing , request.Month , request.Year));

            var act = async () => await CreateUseCase(user , category , goalRepository , PlanLimits.Free).Execute(request , CancellationToken.None);

            if ( blocked ) {
                var error = (await act.Should().ThrowAsync<PlanLimitException>()).Which;
                error.ErrorCode.Should().Be(PlanLimitException.CODE);
                error.GetErrors().Should().ContainSingle().Which.Should().Contain("3");
            } else {
                await act.Should().NotThrowAsync();
            }
        }

        [Fact]
        public async Task Error_Duplicated_Goal ()
        {
            var user = UserBuilder.Build();
            var category = CategoryBuilder.Build(user);
            var request = RequestFinancialGoalJsonBuilder.BuildRegister(category.Id);

            var goalRepository = new FinancialGoalReadOnlyRepositoryBuilder()
                .Exists(user , category.Id , request.Month , request.Year);

            var useCase = CreateUseCase(user , category , goalRepository);

            var act = async () => await useCase.Execute(request , CancellationToken.None);

            (await act.Should().ThrowAsync<BadRequestException>())
                .Which.GetErrors().Should().ContainSingle()
                .Which.Should().Be(ResourceErrorMessages.CATEGORY_MONTHLY_GOAL_DUPLICATED);
        }

        [Fact]
        public async Task Error_Category_Not_Found ()
        {
            var user = UserBuilder.Build();
            var request = RequestFinancialGoalJsonBuilder.BuildRegister(categoryId: 999);

            var useCase = CreateUseCase(user , category: null);

            var act = async () => await useCase.Execute(request , CancellationToken.None);

            (await act.Should().ThrowAsync<NotFoundException>())
                .Which.GetErrors().Should().ContainSingle()
                .Which.Should().Be(ResourceErrorMessages.CATEGORY_NOT_FOUND);
        }

        [Fact]
        public async Task Error_Category_Inactive ()
        {
            var user = UserBuilder.Build();
            var category = CategoryBuilder.Build(user , isActive: false);
            var request = RequestFinancialGoalJsonBuilder.BuildRegister(category.Id);

            var useCase = CreateUseCase(user , category);

            var act = async () => await useCase.Execute(request , CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Error_Invalid_Request ()
        {
            var user = UserBuilder.Build();
            var category = CategoryBuilder.Build(user);
            var request = RequestFinancialGoalJsonBuilder.BuildRegister(category.Id);
            request.TargetAmount = 0;

            var useCase = CreateUseCase(user , category);

            var act = async () => await useCase.Execute(request , CancellationToken.None);

            (await act.Should().ThrowAsync<ErrorOnValidationException>())
                .Which.GetErrors().Should().ContainSingle()
                .Which.Should().Be(ResourceErrorMessages.TARGET_AMOUNT_MUST_BE_GREATER_THAN_ZERO);
        }

        private static RegisterFinancialGoalUseCase CreateUseCase (
            User user ,
            Category? category ,
            FinancialGoalReadOnlyRepositoryBuilder? goalReadOnlyRepository = null ,
            PlanLimits? plan = null )
        {
            return new RegisterFinancialGoalUseCase(
                new FinancialGoalWriteOnlyRepositoryBuilder().Build() ,
                ( goalReadOnlyRepository ?? new FinancialGoalReadOnlyRepositoryBuilder() ).Build() ,
                new CategoriesReadOnlyRepositoryBuilder().GetById(user , category).Build() ,
                LoggedUserBuilder.Build(user) ,
                MapperBuilder.Build() ,
                UnitOfWorkBuilder.Build() ,
                new FinancialCacheInvalidatorBuilder().Build() ,
                UserPlanServiceBuilder.Build(plan));
        }
    }
}
