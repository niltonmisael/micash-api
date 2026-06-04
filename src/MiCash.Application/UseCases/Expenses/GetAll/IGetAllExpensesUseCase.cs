using MiCash.Communication.Responses;

namespace MiCash.Application.UseCases.Expenses.GetAll
{
    public interface IGetAllExpensesUseCase
    {
        public Task<ResponseExpensesJson> Execute (CancellationToken cancellationToken);
    }
}
