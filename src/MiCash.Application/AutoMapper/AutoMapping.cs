using AutoMapper;
using MiCash.Communication.Requests;
using MiCash.Communication.Requests.Categories;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.Categories;
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
        }

        private void EntityToResponse ()
        {
            CreateMap<Expense , ResponseExpenseJson>();
            CreateMap<Expense , ResponseRegisteredExpenseJson>();
            CreateMap<Expense , ResponseShortExpenseJson>();
            CreateMap<User, ResponseUserProfileJson>();
            CreateMap<Category , ResponseShortCategoryJson>();

        }
    }
}
