using AutoMapper;
using DocumentFormat.OpenXml.Spreadsheet;
using MiCash.Communication.Requests.Subscriptions;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Plans;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Subscriptions.Create
{
    public class CreateSubscriptionUseCase : ICreateSubscriptionUseCase
    {
        private readonly ISubscriptionsWriteOnlyRepository _subscriptionsWriteOnlyRepository;
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;
        private readonly IPlansReadOnlyRepository _plansReadOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IUnitOfWork _unitOfWork;

        public CreateSubscriptionUseCase (
            ISubscriptionsWriteOnlyRepository subscriptionsWriteOnlyRepository,
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository,
            IPlansReadOnlyRepository plansReadOnlyRepository,
            ILoggedUser loggedUser,
            IUnitOfWork unitOfWork)
        {
            _subscriptionsWriteOnlyRepository = subscriptionsWriteOnlyRepository;
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
            _plansReadOnlyRepository = plansReadOnlyRepository;
            _loggedUser = loggedUser;
            _unitOfWork = unitOfWork;
        }
        public async Task ExecuteAsync ( RequestCreateSubscriptionJson request , CancellationToken cancellationToken )
        {
            var loggedUser = await _loggedUser.Get();

            var plan = await _plansReadOnlyRepository.GetByIdAsync(request.PlanId , cancellationToken);

            if( plan is null ) {

                throw new NotFoundException(ResourceErrorMessages.PLAN_NOT_FOUND);
            }

            var activeSubscription = await _subscriptionsReadOnlyRepository.GetActiveByUserIdAsync(loggedUser.Id , cancellationToken);

            if ( activeSubscription is not null ) {

                throw new ErrorOnValidationException([ResourceErrorMessages.USER_ALREADY_HAS_ACTIVE_SUBSCRIPTION]);
            }

            var subscription = new Subscription
            {
                UserId = loggedUser.Id ,
                PlanId = plan.Id ,
                Status = SubscriptionStatus.Pending ,
            };

            await _subscriptionsWriteOnlyRepository.AddAsync(subscription , cancellationToken);

            await _unitOfWork.Commit(cancellationToken);

        }
    }
}
