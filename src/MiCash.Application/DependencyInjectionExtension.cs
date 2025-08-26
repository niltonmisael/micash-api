using MiCash.Application.AutoMapper;
using MiCash.Application.UseCases.Expenses.Delete;
using MiCash.Application.UseCases.Expenses.GetAll;
using MiCash.Application.UseCases.Expenses.GetById;
using MiCash.Application.UseCases.Expenses.Register;
using MiCash.Application.UseCases.Expenses.Reports.Excel;
using MiCash.Application.UseCases.Expenses.Reports.PDF;
using MiCash.Application.UseCases.Expenses.Update;
using MiCash.Application.UseCases.Login;
using MiCash.Application.UseCases.Users.Profile;
using MiCash.Application.UseCases.Users.Register;
using MiCash.Domain.Repositories.Expenses;
using Microsoft.Extensions.DependencyInjection;


namespace MiCash.Infrastructure
{
    public static class DependencyInjectionExtension
    {
        public static void AddApplication (this IServiceCollection services)
        {
            AddUseCase(services);
            AddAutoMapping(services);
        }

        private static void AddAutoMapping (IServiceCollection services)
        {
            services.AddAutoMapper(typeof(AutoMapping));
        }
        private static void AddUseCase (IServiceCollection services)
        {
            services.AddScoped<IRegisterExpenseUseCase , RegisterExpenseUseCase>();
            services.AddScoped<IGetAllExpensesUseCase , GetAllExpensesUseCase>();
            services.AddScoped<IGetExpenseByIdUseCase , GetExpenseByIdUseCase>();
            services.AddScoped<IDeleteExpenseUseCase, DeleteExpenseUseCase>();
            services.AddScoped<IUpdateExpenseUseCase, UpdateExpenseUseCase>();
            services.AddScoped<IGenerateExpensesReportExcelUseCase , GenerateExpensesReportExcelUseCase>();
            services.AddScoped<IGenerateExpensesReportPdfUseCase , GenerateExpensesReportPdfUseCase>();
            services.AddScoped<IRegisterUserUseCase , RegisterUserUseCase>();
            services.AddScoped<IDoLoginUseCase , DoLoginUseCase>();
            services.AddScoped<IGetUserProfileUseCase , GetUserProfileUseCase>();
           
        }
    }
}
