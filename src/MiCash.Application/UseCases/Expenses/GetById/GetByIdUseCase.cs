using AutoMapper;
using MiCash.Communication.Responses;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Application.UseCases.Expenses.GetById
{
    class GetByIdUseCase : IGetByIdUseCase
    {
        private readonly IExpensesReadOnlyRepository _repository;
        private readonly IMapper _mapper;

        public GetByIdUseCase (IExpensesReadOnlyRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }
        public async Task<ResponseExpensesJson> Execute (long id )
        {
            var result = await _repository.GetById ( id );

            if ( result is null ) {

                throw new NotFoundException(ResourseErrorMessages.EXPENSE_NOT_FOUND);
            }

            return _mapper.Map<ResponseExpensesJson>(result);
        }
    }
}
