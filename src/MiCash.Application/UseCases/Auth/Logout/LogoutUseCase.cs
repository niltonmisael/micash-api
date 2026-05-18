using MiCash.Application.UseCases.Login.Logout;
using MiCash.Communication.Requests;
using MiCash.Domain.Services.Cache;

namespace MiCash.Application.UseCases.Auth.Logout
{
    public class LogoutUseCase : ILogoutUseCase
    {
        private readonly IRedisCacheService _redisCacheService;

        public LogoutUseCase (
            IRedisCacheService redisCacheService )
        {
            _redisCacheService = redisCacheService;
        }

        public async Task Execute (
            RequestLogoutJson request )
        {
            await _redisCacheService.RemoveAsync(
                $"refreshToken:{request.RefreshToken}"
            );
        }
    }
}
