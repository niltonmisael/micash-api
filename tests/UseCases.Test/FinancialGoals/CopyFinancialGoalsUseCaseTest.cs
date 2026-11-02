using CommonTestUtilities.Cache;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using FluentAssertions;
using MiCash.Application.UseCases.FinancialGoals.Copy;
using MiCash.Domain.Entities;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using Moq;

namespace UseCases.Test.FinancialGoals
{
    public class CopyFinancialGoalsUseCaseTest
    {
        [Fact]
        public async Task Success_Copies_Each_Goal_Once ()
        {
            var user = UserBuilder.Build();
            var request = RequestFinancialGoalJsonBuilder.BuildCopy();
            var sourceGoals = FinancialGoalBuilder.Collection(user , count: 3 , request.SourceMonth , request.SourceYear);

            var readRepository = new FinancialGoalReadOnlyRepositoryBuilder()
                .GetByPeriod(user , request.SourceMonth , request.SourceYear , sourceGoals);
            var writeRepository = new FinancialGoalWriteOnlyRepositoryBuilder();

            var useCase = CreateUseCase(user , readRepository , writeRepository);

            await useCase.Execute(request , CancellationToken.None);

            writeRepository.GetMock().Verify(
                r => r.AddRangeAsync(
                    It.Is<IEnumerable<FinancialGoal>>(goals =>
                        goals.Count() == 3 &&
                        goals.All(g => g.Month == request.TargetMonth && g.Year == request.TargetYear && g.UserId == user.Id)) ,
                    It.IsAny<CancellationToken>()) ,
                Times.Once);
        }

        [Fact]
        public async Task Success_Skips_Inactive_Categories ()
        {
            var user = UserBuilder.Build();
            var request = RequestFinancialGoalJsonBuilder.BuildCopy();
            var sourceGoals = FinancialGoalBuilder.Collection(user , count: 2 , request.SourceMonth , request.SourceYear);
            sourceGoals[0].Category.IsActive = false;

            var readRepository = new FinancialGoalReadOnlyRepositoryBuilder()
                .GetByPeriod(user , request.SourceMonth , request.SourceYear , sourceGoals);
            var writeRepository = new FinancialGoalWriteOnlyRepositoryBuilder();

            var useCase = CreateUseCase(user , readRepository , writeRepository);

            await useCase.Execute(request , CancellationToken.None);

            writeRepository.GetMock().Verify(
                r => r.AddRangeAsync(
                    It.Is<IEnumerable<FinancialGoal>>(goals => goals.Count() == 1 && goals.First().CategoryId == sourceGoals[1].CategoryId) ,
                    It.IsAny<CancellationToken>()) ,
                Times.Once);
        }

        [Fact]
        public async Task Error_Source_Period_Empty ()
        {
            var user = UserBuilder.Build();
            var request = RequestFinancialGoalJsonBuilder.BuildCopy();

            var useCase = CreateUseCase(user , new FinancialGoalReadOnlyRepositoryBuilder() , new FinancialGoalWriteOnlyRepositoryBuilder());

            var act = async () => await useCase.Execute(request , CancellationToken.None);

            (await act.Should().ThrowAsync<ErrorOnValidationException>())
                .Which.GetErrors().Should().ContainSingle()
                .Which.Should().Be(ResourceErrorMessages.FINANCIAL_GOAL_SOURCE_PERIOD_EMPTY);
        }

        [Fact]
        public async Task Error_Target_Period_Not_Empty ()
        {
            var user = UserBuilder.Build();
            var request = RequestFinancialGoalJsonBuilder.BuildCopy();

            var readRepository = new FinancialGoalReadOnlyRepositoryBuilder()
                .GetByPeriod(user , request.SourceMonth , request.SourceYear , FinancialGoalBuilder.Collection(user , 2 , request.SourceMonth , request.SourceYear))
                .GetByPeriod(user , request.TargetMonth , request.TargetYear , FinancialGoalBuilder.Collection(user , 1 , request.TargetMonth , request.TargetYear));

            var useCase = CreateUseCase(user , readRepository , new FinancialGoalWriteOnlyRepositoryBuilder());

            var act = async () => await useCase.Execute(request , CancellationToken.None);

            (await act.Should().ThrowAsync<ErrorOnValidationException>())
                .Which.GetErrors().Should().ContainSingle()
                .Which.Should().Be(ResourceErrorMessages.FINANCIAL_GOAL_TARGET_PERIOD_NOT_EMPTY);
        }

        private static CopyFinancialGoalsUseCase CreateUseCase (
            User user ,
            FinancialGoalReadOnlyRepositoryBuilder readRepository ,
            FinancialGoalWriteOnlyRepositoryBuilder writeRepository )
        {
            return new CopyFinancialGoalsUseCase(
                readRepository.Build() ,
                writeRepository.Build() ,
                LoggedUserBuilder.Build(user) ,
                UnitOfWorkBuilder.Build() ,
                new FinancialCacheInvalidatorBuilder().Build());
        }
    }
}
