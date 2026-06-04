using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Expenses.Delete
{
    public class DeleteExpenseUseCase : IDeleteExpenseUseCase
    {
        private readonly IExpensesReadOnlyRepository _expensesReadOnlyRepository;
        private readonly IExpensesWriteOnlyRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggedUser _loggedUser;
        public DeleteExpenseUseCase (
            IExpensesWriteOnlyRepository repository, 
            IUnitOfWork unitOfWork, 
            ILoggedUser loggedUser, 
            IExpensesReadOnlyRepository expensesReadOnlyRepository)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _loggedUser = loggedUser;
            _expensesReadOnlyRepository = expensesReadOnlyRepository;
        }
        public async Task Execute(long id, CancellationToken cancellationToken)
        {
            var loggedUser = await _loggedUser.Get();

            var expense = await _expensesReadOnlyRepository.GetById(loggedUser,id, cancellationToken);

            if ( expense == null ) {
                throw new NotFoundException(ResourceErrorMessages.EXPENSE_NOT_FOUND);
            }

            await _repository.Delete(id, cancellationToken);

            await _unitOfWork.Commit(cancellationToken);

        }
    }
}
