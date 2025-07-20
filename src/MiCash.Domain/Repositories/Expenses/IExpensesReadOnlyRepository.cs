using MiCash.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Domain.Repositories.Expenses
{
    public interface IExpensesReadOnlyRepository
    {
        public Task<List<Expense>> GetAll (User user);
        public Task<Expense?> GetById (User user ,long id );
        public Task<List<Expense>> FilterByMonth(DateOnly date);
    }
}
