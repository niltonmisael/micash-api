using MiCash.Communication.Requests;
using MiCash.Communication.Responses;
using MiCash.Domain.Entities;

namespace MiCash.Application.Mappers
{
    internal static class UserMapper
    {
        /// <summary>The password is not copied: the use case stores only its hash.</summary>
        public static User ToEntity ( this RequestRegisterUserJson request ) => new()
        {
            Name = request.Name ,
            Email = request.Email ,
            Phone = request.Phone
        };

        public static ResponseUserProfileJson ToProfileResponse ( this User user ) => new()
        {
            Name = user.Name ,
            Email = user.Email ,
            EmailVerified = user.EmailVerifiedAt != null ,
            Phone = user.Phone
        };
    }
}
