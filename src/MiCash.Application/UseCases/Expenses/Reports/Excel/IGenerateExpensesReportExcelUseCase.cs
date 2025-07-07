using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Application.UseCases.Expenses.Reports.Excel
{
    public interface IGenerateExpensesReportExcelUseCase
    {
        Task<byte[]> Execute ( DateOnly month );
    }
}
