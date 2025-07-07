using AutoMapper;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Expenses;
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
        private readonly IExpensesWriteOnlyRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        public DeleteExpenseUseCase (IExpensesWriteOnlyRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }
        public async Task Execute(long id)
        {
            var result = await _repository.Delete(id);

            if ( result == false ) {
                throw new NotFoundException(ResourseErrorMessages.EXPENSE_NOT_FOUND);
            }

            await _unitOfWork.Commit();

        }
    }
}
