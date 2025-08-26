using AutoMapper;
using MiCash.Communication.Responses;
using MiCash.Domain.Services.LoggedUser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Application.UseCases.Users.Profile
{
    public class GetUserProfileUseCase : IGetUserProfileUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly IMapper _mapper;

        public GetUserProfileUseCase (ILoggedUser loggedUser, IMapper mapper)
        {
            _loggedUser = loggedUser;
            _mapper = mapper;
        }
        public async Task<ResponseUserProfileJson> Execute ()
        {
            var user = await _loggedUser.Get();

            return _mapper.Map<ResponseUserProfileJson>(user);
        }
    }
}
