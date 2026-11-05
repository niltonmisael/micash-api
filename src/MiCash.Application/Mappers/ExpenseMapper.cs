using MiCash.Communication.Requests;
using MiCash.Communication.Responses;
using MiCash.Domain.Entities;

namespace MiCash.Application.Mappers
{
    internal static class ExpenseMapper
    {
        public static Expense ToEntity ( this RequestExpenseJson request )
        {
            var expense = new Expense();
            request.ApplyTo(expense);
            return expense;
        }

        /// <summary>Copies the editable fields of the request onto an existing expense (register and update).</summary>
        public static void ApplyTo ( this RequestExpenseJson request , Expense expense )
        {
            expense.Title = request.Title;
            expense.Description = request.Description;
            expense.ExpenseDate = request.ExpenseDate;
            expense.Amount = request.Amount;
            expense.PaymentType = (Domain.Enums.PaymentType)request.PaymentType;
            expense.Status = (Domain.Enums.ExpenseStatus)request.ExpenseStatus;
            expense.CategoryId = request.CategoryId;
        }

        public static ResponseExpenseJson ToResponse ( this Expense expense ) => new()
        {
            Id = expense.Id ,
            Title = expense.Title ,
            Description = expense.Description ,
            ExpenseDate = expense.ExpenseDate ,
            Amount = expense.Amount ,
            PaymentType = (Communication.Enums.PaymentType)expense.PaymentType
        };

        public static ResponseShortExpenseJson ToShortResponse ( this Expense expense ) => new()
        {
            Id = expense.Id ,
            Title = expense.Title ,
            Amount = expense.Amount
        };

        public static ResponseRegisteredExpenseJson ToRegisteredResponse ( this Expense expense ) => new()
        {
            Title = expense.Title
        };
    }
}
