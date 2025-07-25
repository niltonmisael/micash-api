using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Expenses;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTestUtilities.Repositories
{
    public class ExpenseReadOnlyRepositoryBuilder
    {
        private readonly Mock<IExpensesReadOnlyRepository> _repository;

        public ExpenseReadOnlyRepositoryBuilder ()
        {
            _repository = new Mock<IExpensesReadOnlyRepository>();
        }

        public ExpenseReadOnlyRepositoryBuilder GetAll (User user, List<Expense> expenses)
        {
            _repository.Setup(repository => repository.GetAll(user)).ReturnsAsync(expenses);

            return this;
        }

        public IExpensesReadOnlyRepository Build() => _repository.Object;
    }
}
