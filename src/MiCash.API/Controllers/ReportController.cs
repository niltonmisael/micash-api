using MiCash.Application.UseCases.Expenses.Reports.Excel;
using MiCash.Application.UseCases.Expenses.Reports.PDF;
using MiCash.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace MiCash.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = Roles.PREMIUM)]
    public class ReportController : ControllerBase
    {

        [HttpGet("excel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetExcel ( 
            [FromServices] IGenerateExpensesReportExcelUseCase useCase ,
            [FromQuery] DateOnly month )
        {
            byte[] file = await useCase.Execute(month) ;

            if ( file.Length > 0 ) {
                return File(file , MediaTypeNames.Application.Octet , "report.xlsx");
            }
            return NoContent();
        }

        [HttpGet("pdf")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetPdf (
            [FromServices] IGenerateExpensesReportPdfUseCase useCase ,
            [FromQuery] DateOnly month )
        {
            byte[] file = await useCase.Execute(month);

            if ( file.Length > 0 ) {
                return File(file , MediaTypeNames.Application.Pdf , "report.pdf");
            }
            return NoContent();
        }
    }
}
