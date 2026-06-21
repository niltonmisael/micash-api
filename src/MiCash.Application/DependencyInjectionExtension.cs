using MiCash.Application.AutoMapper;
using MiCash.Application.Jobs.Subscriptions;
using MiCash.Application.UseCases.Auth.Login;
using MiCash.Application.UseCases.Auth.Logout;
using MiCash.Application.UseCases.Categories.Create;
using MiCash.Application.UseCases.Categories.GetAll;
using MiCash.Application.UseCases.Categories.GetById;
using MiCash.Application.UseCases.Categories.Inactivate;
using MiCash.Application.UseCases.Categories.Update;
using MiCash.Application.UseCases.Checkout;
using MiCash.Application.UseCases.Dashboard.GetExpensesByCategory;
using MiCash.Application.UseCases.Dashboards.ExpensesByCategory;
using MiCash.Application.UseCases.Dashboards.LatestExpenses;
using MiCash.Application.UseCases.Dashboards.MonthlySummary;
using MiCash.Application.UseCases.Expenses.Delete;
using MiCash.Application.UseCases.Expenses.GetAll;
using MiCash.Application.UseCases.Expenses.GetById;
using MiCash.Application.UseCases.Expenses.Register;
using MiCash.Application.UseCases.Expenses.Reports.Excel;
using MiCash.Application.UseCases.Expenses.Reports.PDF;
using MiCash.Application.UseCases.Expenses.Update;
using MiCash.Application.UseCases.FinancialGoals.Copy;
using MiCash.Application.UseCases.FinancialGols.Copy;
using MiCash.Application.UseCases.FinancialGols.Delete;
using MiCash.Application.UseCases.FinancialGols.GetById;
using MiCash.Application.UseCases.FinancialGols.GetByPeriod;
using MiCash.Application.UseCases.FinancialGols.History;
using MiCash.Application.UseCases.FinancialGols.Register;
using MiCash.Application.UseCases.FinancialGols.Update;
using MiCash.Application.UseCases.Incomes.GetAll;
using MiCash.Application.UseCases.Incomes.GetById;
using MiCash.Application.UseCases.Incomes.Register;
using MiCash.Application.UseCases.Incomes.Update;
using MiCash.Application.UseCases.Login.Logout;
using MiCash.Application.UseCases.Login.Refresh;
using MiCash.Application.UseCases.Plans.GetAll;
using MiCash.Application.UseCases.Plans.GetById;
using MiCash.Application.UseCases.Subscriptions.GetCurrent;
using MiCash.Application.UseCases.Subscriptions.Reactivate;
using MiCash.Application.UseCases.Users.ChangePassword;
using MiCash.Application.UseCases.Users.Delete;
using MiCash.Application.UseCases.Users.Profile;
using MiCash.Application.UseCases.Users.Register;
using MiCash.Application.UseCases.Users.Update;
using MiCash.Application.UseCases.WebHook;
using MiCash.Application.UseCases.WebHook.Handlers;
using MiCash.Domain.Services.Cache.FinancialInvalidator;
using MiCash.Domain.Services.FinancialGoals;
using MiCash.Domain.Services.FinancialInsights;
using MiCash.Domain.Services.Jobs;
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
            services.AddAutoMapper(config => {
                config.AddMaps(typeof(AutoMapping).Assembly);
            });
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
            services.AddScoped<IUpdateUserUseCase , UpdateUserUseCase>();
            services.AddScoped<IChangePasswordUseCase , ChangePasswordUseCase>();
            services.AddScoped<IDeleteUserAccountUseCase , DeleteUserAccountUseCase>();
            services.AddScoped<ILogoutUseCase , LogoutUseCase>();
            services.AddScoped<IRefreshTokenUseCase , RefreshTokenUseCase>();
            services.AddScoped<ICreateCaterogyUseCase,  CreateCaterogyUseCase>();
            services.AddScoped<IGetAllCategoryUseCase ,  GetAllCategoryUseCase>();
            services.AddScoped<IGetCategoryByIdUseCase , GetCategoryByIdUseCase>();
            services.AddScoped<IUpdateCategoryUseCase , UpdateCategoryUseCase>();
            services.AddScoped<IInactivateCategoryUseCase , InactivateCategoryUseCase>();
            services.AddScoped<IRegisterIncomeUseCase , RegisterIncomeUseCase>();
            services.AddScoped<IGetAllIncomesUseCase , GetAllIncomesUseCase>();
            services.AddScoped<IGetIncomeByIdUseCase , GetIncomeByIdUseCase>();
            services.AddScoped<IUpdateIncomeUseCase , UpdateIncomeUseCase>();
            services.AddScoped<IDeleteExpenseUseCase , DeleteExpenseUseCase>();
            services.AddScoped<IGetMonthlySummaryUseCase , GetMonthlySummaryUseCase>();
            services.AddScoped<IGetExpensesByCategoryUseCase , GetExpensesByCategoryUseCase>();
            services.AddScoped<IGetLatestExpensesUseCase , GetLatestExpensesUseCase>();
            services.AddScoped<IGetAllPlansUseCase , GetAllPlansUseCase>();
            services.AddScoped<IGetPlanByIdUseCase , GetPlanByIdUseCase>();
            services.AddScoped<IGetCurrentSubscriptionUseCase , GetCurrentSubscriptionUseCase>();
            services.AddScoped<ICreateCheckoutUseCase, CreateCheckoutUseCase>();
            services.AddScoped<IPagarMeWebhookHandler , SubscriptionCreatedHandler>();
            services.AddScoped<IPagarMeWebhookHandler , SubscriptionCanceledHandler>();
            services.AddScoped<IPagarMeWebhookHandler , ChargePaidHandler>();
            services.AddScoped<IPagarMeWebhookHandler , ChargePaymentFailedHandler>();
            services.AddScoped<IProcessPagarMeWebhookUseCase , ProcessPagarMeWebhookUseCase>();
            services.AddScoped<ISubscriptionCancellationJob ,SubscriptionCancellationJob>();
            services.AddScoped<IReactivateSubscriptionUseCase, ReactivateSubscriptionUseCase>();
            services.AddScoped<IRegisterFinancialGoalUseCase , RegisterFinancialGoalUseCase>();
            services.AddScoped<ICopyFinancialGoalsUseCase, CopyFinancialGoalsUseCase>();
            services.AddScoped<IDeleteFinancialGoalUseCase, DeleteFinancialGoalUseCase>();
            services.AddScoped<IGetFinancialGoalByIdUseCase , GetFinancialGoalByIdUseCase>();
            services.AddScoped<IGetFinancialGoalByPeriodUseCase , GetFinancialGoalByPeriodUseCase>();
            services.AddScoped<IGetFinancialGoalsHistoryUseCase, GetFinancialGoalsHistoryUseCase>();
            services.AddScoped<IUpdateFinancialGoalUseCase,  UpdateFinancialGoalUseCase>();
            services.AddScoped<IFinancialGoalCalculator , FinancialGoalCalculator>();
            services.AddScoped<IFinancialInsightAnalyzer ,FinancialInsightAnalyzer>();
            services.AddScoped<IFinancialCacheInvalidator ,FinancialCacheInvalidator>();
        }
    }
}
