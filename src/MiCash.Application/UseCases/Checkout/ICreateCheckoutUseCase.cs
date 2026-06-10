using MiCash.Communication.Requests.Checkout;
using MiCash.Communication.Responses.Checkout;

namespace MiCash.Application.UseCases.Checkout
{
    public interface ICreateCheckoutUseCase
    {
        Task<ResponseCheckoutJson> ExecuteAsync ( RequestCreateCheckoutJson request , CancellationToken cancellationToken );
    }
}
