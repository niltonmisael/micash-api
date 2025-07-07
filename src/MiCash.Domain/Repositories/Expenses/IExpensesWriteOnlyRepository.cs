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
        /// <summary>
        /// This function returns TRUE if the deletion was sucessful
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<bool> Delete ( long id );
    }
}
