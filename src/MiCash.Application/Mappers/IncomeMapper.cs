using MiCash.Communication.Requests.Incomes;
using MiCash.Communication.Responses.Incomes;
using MiCash.Domain.Entities;

namespace MiCash.Application.Mappers
{
    internal static class IncomeMapper
    {
        public static Income ToEntity ( this RequestIncomeJson request )
        {
            var income = new Income();
            request.ApplyTo(income);
            return income;
        }

        /// <summary>Copies the editable fields of the request onto an existing income (register and update).</summary>
        public static void ApplyTo ( this RequestIncomeJson request , Income income )
        {
            income.Title = request.Title;
            income.Description = request.Description;
            income.Amount = request.Amount;
            income.ReceivedAt = request.ReceivedAt;
            income.Category = (Domain.Enums.IncomeCategory)request.IncomeCategory;
            income.IsRecurring = request.IsRecurring;
        }

        public static ResponseIncomeJson ToResponse ( this Income income ) => new()
        {
            Id = income.Id ,
            Title = income.Title ,
            Description = income.Description ,
            Amount = income.Amount ,
            ReceivedAt = income.ReceivedAt ,
            Category = (Communication.Enums.IncomeCategory)income.Category
        };

        public static ResponseRegisteredIncomeJson ToRegisteredResponse ( this Income income ) => new()
        {
            Title = income.Title
        };
    }
}
