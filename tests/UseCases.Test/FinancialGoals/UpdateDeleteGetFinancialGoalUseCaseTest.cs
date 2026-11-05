using CommonTestUtilities.Cache;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using FluentAssertions;
using MiCash.Application.UseCases.FinancialGoals.Delete;
using MiCash.Application.UseCases.FinancialGoals.GetById;
using MiCash.Application.UseCases.FinancialGoals.Update;
using MiCash.Domain.Entities;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace UseCases.Test.FinancialGoals
{
    public class UpdateFinancialGoalUseCaseTest
    {
        [Fact]
        public async Task Success ()
        {
            var user = UserBuilder.Build();
            var goal = FinancialGoalBuilder.Build(user);
            var request = RequestFinancialGoalJsonBuilder.BuildUpdate();

            var useCase = CreateUseCase(user , goal);

            await useCase.Execute(goal.Id , request , CancellationToken.None);

            goal.TargetAmount.Should().Be(request.TargetAmount);
        }

        [Fact]
        public async Task Error_Goal_Not_Found ()
        {
            var user = UserBuilder.Build();

            var useCase = CreateUseCase(user , goal: null);

            var act = async () => await useCase.Execute(1000 , RequestFinancialGoalJsonBuilder.BuildUpdate() , CancellationToken.None);

            (await act.Should().ThrowAsync<NotFoundException>())
                .Which.GetErrors().Should().ContainSingle()
                .Which.Should().Be(ResourceErrorMessages.FINANCIAL_GOAL_NOT_FOUND);
        }

        [Fact]
        public async Task Error_Invalid_Target_Amount ()
        {
            var user = UserBuilder.Build();
            var goal = FinancialGoalBuilder.Build(user);
            var request = RequestFinancialGoalJsonBuilder.BuildUpdate();
            request.TargetAmount = -1;

            var useCase = CreateUseCase(user , goal);

            var act = async () => await useCase.Execute(goal.Id , request , CancellationToken.None);

            await act.Should().ThrowAsync<ErrorOnValidationException>();
        }

        private static UpdateFinancialGoalUseCase CreateUseCase ( User user , FinancialGoal? goal )
        {
            return new UpdateFinancialGoalUseCase(
                new FinancialGoalUpdateOnlyRepositoryBuilder().GetById(user , goal).Build() ,
                LoggedUserBuilder.Build(user) ,
                UnitOfWorkBuilder.Build() ,
                new FinancialCacheInvalidatorBuilder().Build());
        }
    }

    public class DeleteFinancialGoalUseCaseTest
    {
        [Fact]
        public async Task Success ()
        {
            var user = UserBuilder.Build();
            var goal = FinancialGoalBuilder.Build(user);
            var writeRepository = new FinancialGoalWriteOnlyRepositoryBuilder();

            var useCase = CreateUseCase(user , goal , writeRepository);

            await useCase.Execute(goal.Id , CancellationToken.None);

            writeRepository.GetMock().Verify(r => r.Delete(goal) , Moq.Times.Once);
        }

        [Fact]
        public async Task Error_Goal_From_Another_User ()
        {
            var owner = UserBuilder.Build();
            var goal = FinancialGoalBuilder.Build(owner);

            var anotherUser = UserBuilder.Build();
            anotherUser.Id = 2;

            // The repository only returns goals owned by the given user id.
            var useCase = CreateUseCase(anotherUser , goal: null , new FinancialGoalWriteOnlyRepositoryBuilder());

            var act = async () => await useCase.Execute(goal.Id , CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        private static DeleteFinancialGoalUseCase CreateUseCase ( User user , FinancialGoal? goal , FinancialGoalWriteOnlyRepositoryBuilder writeRepository )
        {
            return new DeleteFinancialGoalUseCase(
                writeRepository.Build() ,
                new FinancialGoalUpdateOnlyRepositoryBuilder().GetById(user , goal).Build() ,
                LoggedUserBuilder.Build(user) ,
                UnitOfWorkBuilder.Build() ,
                new FinancialCacheInvalidatorBuilder().Build());
        }
    }

    public class GetFinancialGoalByIdUseCaseTest
    {
        [Fact]
        public async Task Success ()
        {
            var user = UserBuilder.Build();
            var goal = FinancialGoalBuilder.Build(user);

            var useCase = CreateUseCase(user , goal);

            var response = await useCase.Execute(goal.Id , CancellationToken.None);

            response.Id.Should().Be(goal.Id);
            response.CategoryId.Should().Be(goal.CategoryId);
            response.CategoryName.Should().Be(goal.Category.Name);
            response.TargetAmount.Should().Be(goal.TargetAmount);
            response.Month.Should().Be(goal.Month);
            response.Year.Should().Be(goal.Year);
        }

        [Fact]
        public async Task Error_Goal_Not_Found ()
        {
            var user = UserBuilder.Build();

            var useCase = CreateUseCase(user , goal: null);

            var act = async () => await useCase.Execute(1000 , CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        private static GetFinancialGoalByIdUseCase CreateUseCase ( User user , FinancialGoal? goal )
        {
            return new GetFinancialGoalByIdUseCase(
                LoggedUserBuilder.Build(user) ,
                new FinancialGoalReadOnlyRepositoryBuilder().GetById(user , goal).Build() ,
                new FakeRedisCacheService());
        }
    }
}
