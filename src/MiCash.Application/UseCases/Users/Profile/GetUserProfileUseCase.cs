using MiCash.Application.Mappers;
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

        public GetUserProfileUseCase (ILoggedUser loggedUser)
        {
            _loggedUser = loggedUser;
        }
        public async Task<ResponseUserProfileJson> Execute ()
        {
            var user = await _loggedUser.Get();

            return user.ToProfileResponse();
        }
    }
}
