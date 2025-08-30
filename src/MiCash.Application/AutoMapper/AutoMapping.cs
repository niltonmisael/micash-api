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
    public class AutoMapping : Profile
    {
        public AutoMapping ()
        {
            RequestToEntity();
            EntityToResponse();
        }
        private void RequestToEntity ()
        {
            CreateMap<RequestExpenseJson , Expense>()
                .ForMember(dest => dest.Tags , config => config.MapFrom(source => source.Tags.Distinct()));

            CreateMap<RequestRegisterUserJson , User>();

            CreateMap<Communication.Enums.Tag , Tag>()
                .ForMember(dest => dest.Value , config => config.MapFrom(source => source));
        

        }

        private void EntityToResponse ()
        {
            CreateMap<Expense , ResponseExpenseJson>()
                .ForMember(dest => dest.Tags , config => config.MapFrom(source => source.Tags.Select(tag => tag.Value)));

            CreateMap<Expense , ResponseRegisteredExpenseJson>();
            CreateMap<Expense , ResponseShortExpenseJson>();
            CreateMap<User, ResponseUserProfileJson>();

        }
    }
}
