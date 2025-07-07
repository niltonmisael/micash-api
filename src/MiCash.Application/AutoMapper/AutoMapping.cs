using AutoMapper;
using MiCash.Communication.Requests;
using MiCash.Communication.Responses;
using MiCash.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Application.AutoMapper
{
    class AutoMapping : Profile
    {
        public AutoMapping ()
        {
            RequestToEntity();
            EntityToResponse();
        }
        private void RequestToEntity ()
        {
            CreateMap<RequestExpenseJson , Expense>();

        }

        private void EntityToResponse ()
        {
            CreateMap<Expense , ResponseRegisteredExpenseJson>();
            CreateMap<Expense , ResponseShortExpenseJson>();
            CreateMap<Expense , ResponseExpensesJson>();

        }
    }
}
