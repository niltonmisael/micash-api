using MiCash.Communication.Requests.Notifications;
using MiCash.Communication.Responses.Notifications;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.PushNotifications;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using System.Text.Json;

namespace MiCash.Application.UseCases.Notifications
{
    public interface IGetNotificationsUseCase
    {
        Task<ResponseNotificationsJson> Execute ( RequestNotificationsJson request , CancellationToken cancellationToken );
    }

    public interface IGetUnreadNotificationsCountUseCase
    {
        Task<ResponseUnreadNotificationsCountJson> Execute ( CancellationToken cancellationToken );
    }

    public interface IMarkNotificationAsReadUseCase
    {
        Task Execute ( long id , CancellationToken cancellationToken );
    }

    public interface IMarkAllNotificationsAsReadUseCase
    {
        Task Execute ( CancellationToken cancellationToken );
    }

    /// <summary>The logged user's notification center, newest first, paged by cursor (same scheme as the chat).</summary>
    public sealed class GetNotificationsUseCase : IGetNotificationsUseCase
    {
        public const int DEFAULT_LIMIT = 20;
        public const int MAX_LIMIT = 50;

        private readonly ILoggedUser _loggedUser;
        private readonly INotificationCenterReadOnlyRepository _repository;

        public GetNotificationsUseCase ( ILoggedUser loggedUser , INotificationCenterReadOnlyRepository repository )
        {
            _loggedUser = loggedUser;
            _repository = repository;
        }

        public async Task<ResponseNotificationsJson> Execute ( RequestNotificationsJson request , CancellationToken cancellationToken )
        {
            var limit = request.Limit <= 0 ? DEFAULT_LIMIT : Math.Min(request.Limit , MAX_LIMIT);

            var user = await _loggedUser.Get();

            // One extra row tells whether there is an older page.
            var page = await _repository.GetPageAsync(user.Id , request.Before , limit + 1 , cancellationToken);

            return new ResponseNotificationsJson
            {
                HasMore = page.Count > limit ,
                Notifications = page.Take(limit).Select(Map).ToList()
            };
        }

        private static ResponseNotificationJson Map ( PushNotification notification ) => new()
        {
            Id = notification.Id ,
            Type = notification.Type ,
            Title = notification.Title ,
            Body = notification.Body ,
            Url = ReadUrl(notification.Data) ,
            CreatedAt = notification.CreatedAt ,
            ReadAt = notification.ReadAt
        };

        /// <summary>Only the route from Data: the app does not need to know the internal format.</summary>
        private static string? ReadUrl ( string data )
        {
            try {
                var values = JsonSerializer.Deserialize<Dictionary<string , string>>(data);
                return values is not null && values.TryGetValue("url" , out var url) ? url : null;
            } catch ( JsonException ) {
                return null;
            }
        }
    }

    public sealed class GetUnreadNotificationsCountUseCase : IGetUnreadNotificationsCountUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly INotificationCenterReadOnlyRepository _repository;

        public GetUnreadNotificationsCountUseCase ( ILoggedUser loggedUser , INotificationCenterReadOnlyRepository repository )
        {
            _loggedUser = loggedUser;
            _repository = repository;
        }

        public async Task<ResponseUnreadNotificationsCountJson> Execute ( CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            return new ResponseUnreadNotificationsCountJson
            {
                Count = await _repository.CountUnreadAsync(user.Id , cancellationToken)
            };
        }
    }

    /// <summary>Idempotent: an already read notification keeps its original ReadAt. Another user's → 404.</summary>
    public sealed class MarkNotificationAsReadUseCase : IMarkNotificationAsReadUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly INotificationCenterUpdateOnlyRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _time;

        public MarkNotificationAsReadUseCase (
            ILoggedUser loggedUser ,
            INotificationCenterUpdateOnlyRepository repository ,
            IUnitOfWork unitOfWork ,
            TimeProvider time )
        {
            _loggedUser = loggedUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
            _time = time;
        }

        public async Task Execute ( long id , CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var notification = await _repository.GetByIdAsync(user.Id , id , cancellationToken)
                ?? throw new NotFoundException(ResourceErrorMessages.NOTIFICATION_NOT_FOUND);

            if ( notification.ReadAt is not null ) {
                return;
            }

            notification.ReadAt = _time.GetUtcNow().UtcDateTime;

            await _unitOfWork.Commit(cancellationToken);
        }
    }

    /// <summary>Marks every unread notification of the user with the same time, in a single save.</summary>
    public sealed class MarkAllNotificationsAsReadUseCase : IMarkAllNotificationsAsReadUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly INotificationCenterUpdateOnlyRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _time;

        public MarkAllNotificationsAsReadUseCase (
            ILoggedUser loggedUser ,
            INotificationCenterUpdateOnlyRepository repository ,
            IUnitOfWork unitOfWork ,
            TimeProvider time )
        {
            _loggedUser = loggedUser;
            _repository = repository;
            _unitOfWork = unitOfWork;
            _time = time;
        }

        public async Task Execute ( CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var unread = await _repository.GetUnreadAsync(user.Id , cancellationToken);

            if ( unread.Count == 0 ) {
                return;
            }

            var now = _time.GetUtcNow().UtcDateTime;

            foreach ( var notification in unread ) {
                notification.ReadAt = now;
            }

            await _unitOfWork.Commit(cancellationToken);
        }
    }
}
