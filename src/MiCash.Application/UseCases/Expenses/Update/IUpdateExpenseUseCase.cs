using MiCash.Communication.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Application.UseCases.Expenses.Update
{
    public interface IUpdateExpenseUseCase
    {
        Task Execute ( long id , RequestExpenseJson request );
    }
}
