using AutoMapper;
using MiCash.Communication.Requests;
using MiCash.Communication.Requests.Categories;
using MiCash.Communication.Requests.Incomes;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.Categories;
using MiCash.Communication.Responses.Incomes;
using MiCash.Domain.Entities;

namespace MiCash.Application.AutoMapper
{
    public class AutoMapping : Profile
    {
        public AutoMapping ()
        {
            RequestToEntity();
            EntityToResponse();
        }
        private void RequestToEntity ()
        {

            CreateMap<RequestRegisterUserJson , User>();
            CreateMap<RequestCreateCategoryJson , Category>();
            CreateMap<RequestExpenseJson , Expense>();
            CreateMap<RequestIncomeJson , Income>();
        }

        private void EntityToResponse ()
        {
            CreateMap<Expense , ResponseExpenseJson>();
            CreateMap<Expense , ResponseRegisteredExpenseJson>();
            CreateMap<Expense , ResponseShortExpenseJson>();
            CreateMap<User, ResponseUserProfileJson>();
            CreateMap<Category , ResponseShortCategoryJson>();
            CreateMap<Category , ResponseCategoryJson>();
            CreateMap<Category , ResponseCategoriesJson>();
            CreateMap<Income , ResponseIncomesJson>();
            CreateMap<Income , ResponseRegisteredIncomeJson>();
            CreateMap<Income , ResponseIncomeJson>();
            CreateMap<Income , ResponseIncomeShortJson>();
        }
    }
}
