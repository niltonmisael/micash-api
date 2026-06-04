using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Mapper;
using CommonTestUtilities.Repositories;
using FluentAssertions;
using MiCash.Application.UseCases.Expenses.Update;
using MiCash.Domain.Entities;
using MiCash.Exception.ExceptionBase;
using MiCash.Exception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTestUtilities.Requests;
using MiCash.Domain.Enums;
using CommonTestUtilities.Cache;

namespace UseCases.Test.Expenses.Update
{
    public class UpdateExpenseUseCaseTest
    {

        [Fact]
        public async Task Success ()
        {
            var cancellationToken = CancellationToken.None;
            var loggedUser = UserBuilder.Build();
            var request = RequestExpenseJsonBuilder.Build();
            var expense = ExpenseBuilder.Build(loggedUser);

            var useCase = CreateUseCase(loggedUser , expense);

            var act = async () => await useCase.Execute(expense.Id , request, cancellationToken);

            await act.Should().NotThrowAsync();

            expense.Title.Should().Be(request.Title);
            expense.Description.Should().Be(request.Description);
            expense.ExpenseDate.Should().Be(request.ExpenseDate);
            expense.Amount.Should().Be(request.Amount);
            expense.PaymentType.Should().Be(( PaymentType ) request.PaymentType);
        }

        [Fact]
        public async Task Error_Title_Empty ()
        {
            var cancellationToken = CancellationToken.None;
            var loggedUser = UserBuilder.Build();
            var expense = ExpenseBuilder.Build(loggedUser);

            var request = RequestExpenseJsonBuilder.Build();
            request.Title = string.Empty;

            var useCase = CreateUseCase(loggedUser , expense);

            var act = async () => await useCase.Execute(expense.Id , request, cancellationToken);

            var result = await act.Should().ThrowAsync<ErrorOnValidationException>();

            result.Where(ex => ex.GetErrors().Count == 1 && ex.GetErrors().Contains(ResourceErrorMessages.TITLE_REQUIRED));
        }

        [Fact]
        public async Task Error_Expense_Not_Found ()
        {
            var cancellationToken = CancellationToken.None;
            var loggedUser = UserBuilder.Build();
            var request = RequestExpenseJsonBuilder.Build();

            var useCase = CreateUseCase(loggedUser);

            var act = async () => await useCase.Execute(id: 10000 , request, cancellationToken);

            var result = await act.Should().ThrowAsync<NotFoundException>();

            result.Where(ex => ex.GetErrors().Count == 1 && ex.GetErrors().Contains(ResourceErrorMessages.EXPENSE_NOT_FOUND));
        }

        private UpdateExpenseUseCase CreateUseCase ( User user , Expense? expense = null )
        {
            var repository = new ExpensesUpdateOnlyRepositoryBuilder().GetById(user , expense).Build();
            var mapper = MapperBuilder.Build();
            var unitOfWork = UnitOfWorkBuilder.Build();
            var loggedUser = LoggedUserBuilder.Build(user);
            var cacheService = new RedisCacheServiceBuilder().Build();

            return new UpdateExpenseUseCase(mapper , unitOfWork , repository , loggedUser, cacheService);
        }
    }
}
