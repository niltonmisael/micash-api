namespace MiCash.Application.UseCases.Expenses.Reports.PDF
{
    public interface IGenerateExpensesReportPdfUseCase
    {
        Task<byte[]> Execute ( DateOnly month, CancellationToken cancellationToken );
    }
}
