using CommonTestUtilities.Cache;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Mapper;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using FluentAssertions;
using MiCash.Application.Services.FinancialGoals;
using MiCash.Application.UseCases.Expenses.Register;
using MiCash.Domain.Entities;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using Moq;

namespace UseCases.Test.Expenses.Register
{
    public class RegisterExpenseUseCaseTest
    {
        [Fact]
        public async Task Success ()
        {
            var cancellationToken = CancellationToken.None;
            var loggedUser = UserBuilder.Build();
            var request = RequestExpenseJsonBuilder.Build();
            var useCase = CreateUseCase(loggedUser);

            var result = await useCase.Execute(request, cancellationToken);

            result.Should().NotBeNull();
            result.Title.Should().Be(request.Title);

        }

        [Fact]
        public async Task Success_Checks_The_Goal_Alert_Of_The_Category ()
        {
            var loggedUser = UserBuilder.Build();
            var request = RequestExpenseJsonBuilder.Build();
            var notifier = new Mock<IFinancialGoalAlertNotifier>();

            await CreateUseCase(loggedUser , notifier).Execute(request , CancellationToken.None);

            notifier.Verify(n => n.NotifyAsync(loggedUser , request.CategoryId , It.IsAny<DateTime>() , It.IsAny<CancellationToken>()) , Times.Once);
        }

        [Fact]
        public async Task Error_Title_Empty ()
        {
            var cancellationToken = CancellationToken.None;
            var loggedUser = UserBuilder.Build();
            var request = RequestExpenseJsonBuilder.Build();
            request.Title = string.Empty;

            var useCase = CreateUseCase(loggedUser);

            var act = async () => await useCase.Execute(request, cancellationToken);

            var result = act.Should().ThrowAsync<ErrorOnValidationException>();

            await result.Where(ex => ex.GetErrors().Count == 1 && ex.GetErrors().Contains(ResourceErrorMessages.TITLE_REQUIRED));

        }

        private RegisterExpenseUseCase CreateUseCase(User user , Mock<IFinancialGoalAlertNotifier>? goalAlertNotifier = null )
        {
            var repository = ExpenseWriteOnlyRepositoryBuilder.Build();
            var mapper = MapperBuilder.Build();
            var unitOfWork = UnitOfWorkBuilder.Build();
            var loggedUser = LoggedUserBuilder.Build(user);
            var cacheService = new RedisCacheServiceBuilder().Build();
            var financialInvalidator = new FinancialCacheInvalidatorBuilder().Build();

            return new RegisterExpenseUseCase(repository,unitOfWork, mapper, loggedUser, cacheService, financialInvalidator, ( goalAlertNotifier ?? new Mock<IFinancialGoalAlertNotifier>() ).Object);
        }
    }
}
