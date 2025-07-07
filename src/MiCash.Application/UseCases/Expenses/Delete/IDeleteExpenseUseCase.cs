using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Application.UseCases.Expenses.Delete
{
    public interface IDeleteExpenseUseCase
    {
        Task Execute(long id);
    }
}
