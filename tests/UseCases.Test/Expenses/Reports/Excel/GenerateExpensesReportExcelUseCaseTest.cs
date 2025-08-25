using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using FluentAssertions;
using MiCash.Application.UseCases.Expenses.Reports.Excel;
using MiCash.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UseCases.Test.Expenses.Reports.Excel
{
    public class GenerateExpensesReportExcelUseCaseTest
    {
        [Fact]
        public async Task Success ()
        {
            var loggedUser = UserBuilder.Build();
            var expenses = ExpenseBuilder.Collection(loggedUser);

            var useCase = CreatedUseCase(loggedUser, expenses);

            var result = await useCase.Execute(DateOnly.FromDateTime(DateTime.Today));

            result.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Success_Empty ()
        {
            var loggedUser = UserBuilder.Build();

            var useCase = CreatedUseCase(loggedUser , new List<Expense>());

            var result = await useCase.Execute(DateOnly.FromDateTime(DateTime.Today));

            result.Should().BeEmpty();
        }

        private GenerateExpensesReportExcelUseCase CreatedUseCase (User user, List<Expense> expenses)
        {
            var repository = new ExpenseReadOnlyRepositoryBuilder().FilterByMonth(user, expenses).Build();
            var loggedUser = LoggedUserBuilder.Build(user);

            return new GenerateExpensesReportExcelUseCase(repository, loggedUser);

        }
    }
}
