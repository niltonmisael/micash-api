using CommonTestUtilities.Cache;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Mapper;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using FluentAssertions;
using MiCash.Application.UseCases.Expenses.Register;
using MiCash.Domain.Entities;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

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

        private RegisterExpenseUseCase CreateUseCase(User user )
        {
            var repository = ExpenseWriteOnlyRepositoryBuilder.Build();
            var mapper = MapperBuilder.Build();
            var unitOfWork = UnitOfWorkBuilder.Build();
            var loggedUser = LoggedUserBuilder.Build(user);
            var cacheService = new RedisCacheServiceBuilder().Build();

            return new RegisterExpenseUseCase(repository,unitOfWork, mapper, loggedUser, cacheService);
        }
    }
}
