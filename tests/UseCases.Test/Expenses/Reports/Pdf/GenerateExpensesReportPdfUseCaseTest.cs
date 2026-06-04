using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using FluentAssertions;
using MiCash.Application.UseCases.Expenses.Reports.PDF;
using MiCash.Domain.Entities;

namespace UseCases.Test.Expenses.Reports.Pdf
{
    public class GenerateExpensesReportPdfUseCaseTest
    {

        [Fact]
        public async Task Success ()
        {
            var cancellationToken = CancellationToken.None;
            var loggedUser = UserBuilder.Build();
            var expenses = ExpenseBuilder.Collection(loggedUser);

            var useCase = CreatedUseCase(loggedUser , expenses);

            var result = await useCase.Execute(DateOnly.FromDateTime(DateTime.Today), cancellationToken);

            result.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Success_Empty ()
        {
            var cancellationToken = CancellationToken.None;
            var loggedUser = UserBuilder.Build();

            var useCase = CreatedUseCase(loggedUser , []);

            var result = await useCase.Execute(DateOnly.FromDateTime(DateTime.Today), cancellationToken);

            result.Should().BeEmpty();
        }

        private GenerateExpensesReportPdfUseCase CreatedUseCase ( User user , List<Expense> expenses )
        {
            var repository = new ExpenseReadOnlyRepositoryBuilder().FilterByMonth(user , expenses).Build();
            var loggedUser = LoggedUserBuilder.Build(user);

            return new GenerateExpensesReportPdfUseCase(repository , loggedUser);

        }
    }
}
