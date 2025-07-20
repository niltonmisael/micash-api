using MiCash.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Domain.Repositories.Expenses
{
    public interface IExpensesWriteOnlyRepository
    {
        public Task Add ( Expense expense );
       
        public Task Delete ( long id );
    }
}
