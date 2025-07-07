using FluentValidation;
using MiCash.Communication.Requests;
using MiCash.Exception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Application.UseCases.Expenses
{
    public class ExpensesValidator : AbstractValidator<RequestExpenseJson>
    {
        public ExpensesValidator ()
        {
            RuleFor(expense => expense.Title).NotEmpty().WithMessage(ResourseErrorMessages.TITLE_REQUIRED);
            RuleFor(expense => expense.Amount).GreaterThan(0).WithMessage(ResourseErrorMessages.AMOUNT_MUST_BE_GREATER_THAN_ZERO);
            RuleFor(expense => expense.Date).LessThanOrEqualTo(DateTime.UtcNow).WithMessage(ResourseErrorMessages.EXPENSES_CANNOT_FOR_THE_FUTURE);
            RuleFor(expense => expense.PaymentType).IsInEnum().WithMessage(ResourseErrorMessages.PAYMENT_TYPE_INVALID);
        }
    }
}
