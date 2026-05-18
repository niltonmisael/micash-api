using FluentValidation;
using MiCash.Communication.Requests.Categories;
using MiCash.Exception;

namespace MiCash.Application.UseCases.Categories
{
    public class CategoryValidator : AbstractValidator<RequestCreateCategoryJson>
    {
        public CategoryValidator ()
        {
            RuleFor(category => category.Name).NotEmpty().WithMessage(ResourceErrorMessages.NAME_EMPTY);
            RuleFor(category => category.Color).NotEmpty().WithMessage(ResourceErrorMessages.COLOR_REQUIRED);
            RuleFor(category => category.Icon).NotEmpty().WithMessage(ResourceErrorMessages.ICON_REQUIRED);
        }
    }
}
