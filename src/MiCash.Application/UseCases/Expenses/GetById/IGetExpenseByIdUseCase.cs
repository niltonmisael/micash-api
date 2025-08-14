using MiCash.Communication.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Application.UseCases.Expenses.GetById
{
    public interface IGetExpenseByIdUseCase
    {
        public Task<ResponseExpenseJson> Execute(long id);   
    }
}
