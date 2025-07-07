using MiCash.Communication.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Application.UseCases.Expenses.GetAll
{
    public interface IGetAllExpensesUseCase
    {
        public Task<ResponseExpensesJson> Execute ();
    }
}
