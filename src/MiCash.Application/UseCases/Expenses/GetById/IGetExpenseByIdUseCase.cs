using MiCash.Communication.Responses;

namespace MiCash.Application.UseCases.Expenses.GetById
{
    public interface IGetExpenseByIdUseCase
    {
        public Task<ResponseExpenseJson> Execute(long id, CancellationToken cancellationToken);   
    }
}
