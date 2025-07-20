using AutoMapper;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Application.UseCases.Expenses.Delete
{
    public class DeleteExpenseUseCase : IDeleteExpenseUseCase
    {
        private readonly IExpensesReadOnlyRepository _expensesReadOnlyRepository;
        private readonly IExpensesWriteOnlyRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggedUser _loggedUser;
        public DeleteExpenseUseCase (IExpensesWriteOnlyRepository repository, IUnitOfWork unitOfWork, ILoggedUser loggedUser, IExpensesReadOnlyRepository expensesReadOnlyRepository)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _loggedUser = loggedUser;
            _expensesReadOnlyRepository = expensesReadOnlyRepository;
        }
        public async Task Execute(long id)
        {
            var loggedUser = await _loggedUser.Get();

            var expense = await _expensesReadOnlyRepository.GetById(loggedUser,id);

            if ( expense == null ) {
                throw new NotFoundException(ResourseErrorMessages.EXPENSE_NOT_FOUND);
            }

            await _repository.Delete(id);

            await _unitOfWork.Commit();

        }
    }
}
