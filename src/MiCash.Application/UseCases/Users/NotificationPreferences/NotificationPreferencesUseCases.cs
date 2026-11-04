using MiCash.Communication.Requests.Users;
using MiCash.Communication.Responses.Users;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.Users.NotificationPreferences
{
    public interface IGetNotificationPreferencesUseCase
    {
        Task<ResponseNotificationPreferencesJson> Execute ();
    }

    public interface IUpdateNotificationPreferencesUseCase
    {
        Task<ResponseNotificationPreferencesJson> Execute ( RequestNotificationPreferencesJson request , CancellationToken cancellationToken );
    }

    /// <summary>GET api/user/notification-preferences.</summary>
    public sealed class GetNotificationPreferencesUseCase : IGetNotificationPreferencesUseCase
    {
        private readonly ILoggedUser _loggedUser;

        public GetNotificationPreferencesUseCase ( ILoggedUser loggedUser )
        {
            _loggedUser = loggedUser;
        }

        public async Task<ResponseNotificationPreferencesJson> Execute () =>
            NotificationPreferences.ToResponse(await _loggedUser.Get());
    }

    /// <summary>PUT api/user/notification-preferences: replaces the three switches at once.</summary>
    public sealed class UpdateNotificationPreferencesUseCase : IUpdateNotificationPreferencesUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly IUserUpdateOnlyRepository _users;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateNotificationPreferencesUseCase ( ILoggedUser loggedUser , IUserUpdateOnlyRepository users , IUnitOfWork unitOfWork )
        {
            _loggedUser = loggedUser;
            _users = users;
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseNotificationPreferencesJson> Execute (
            RequestNotificationPreferencesJson request ,
            CancellationToken cancellationToken )
        {
            var loggedUser = await _loggedUser.Get();
            var user = await _users.GetById(loggedUser.Id);

            user.MonthlySummaryEmail = request.MonthlySummaryEmail;
            user.MonthlySummaryPush = request.MonthlySummaryPush;
            user.NewsEmail = request.NewsEmail;
            user.UpdatedAt = DateTime.UtcNow;

            _users.Update(user);
            await _unitOfWork.Commit(cancellationToken);

            return NotificationPreferences.ToResponse(user);
        }
    }

    internal static class NotificationPreferences
    {
        public static ResponseNotificationPreferencesJson ToResponse ( User user ) => new()
        {
            MonthlySummaryEmail = user.MonthlySummaryEmail ,
            MonthlySummaryPush = user.MonthlySummaryPush ,
            NewsEmail = user.NewsEmail
        };
    }
}
