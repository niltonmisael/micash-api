namespace MiCash.Application.UseCases.Expenses.Reports.Excel
{
    public interface IGenerateExpensesReportExcelUseCase
    {
        Task<byte[]> Execute ( DateOnly month, CancellationToken cancellationToken );
    }
}
