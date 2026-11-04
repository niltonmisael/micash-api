using MiCash.API.Filters;
using MiCash.Application.UseCases.FinancialGoals.Alert;
using MiCash.Application.UseCases.FinancialGoals.Copy;
using MiCash.Application.UseCases.FinancialGoals.Delete;
using MiCash.Application.UseCases.FinancialGoals.GetById;
using MiCash.Application.UseCases.FinancialGoals.GetByPeriod;
using MiCash.Application.UseCases.FinancialGoals.History;
using MiCash.Application.UseCases.FinancialGoals.Progress;
using MiCash.Application.UseCases.FinancialGoals.Register;
using MiCash.Application.UseCases.FinancialGoals.Update;
using MiCash.Communication.Pagination;
using MiCash.Communication.Requests.FinancialGoals;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.FinancialGoals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MiCash.API.Controllers
{
    [Route("api/financial-goal")]
    [SwaggerTag("Endpoints responsáveis pelo gerenciamento das metas financeiras do usuário.")]
    [ApiController]
    [Authorize]
    public class FinancialGoalController : ControllerBase
    {
        [HttpPost]
        [SwaggerOperation(
            Summary = "Criar uma meta financeira" ,
            Description = "Cria uma nova meta financeira (limite de gastos) para uma categoria do usuário autenticado em um mês/ano."
        )]
        [ProducesResponseType(typeof(ResponseRegisteredFinancialGoalJson) , StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Register (
            [FromServices] IRegisterFinancialGoalUseCase useCase ,
            [FromBody] RequestRegisterFinancialGoal request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(request , cancellationToken);

            return Created(string.Empty , response);
        }

        [HttpGet]
        [PlanHistoryLimit]
        [SwaggerOperation(
            Summary = "Listar metas financeiras por período" ,
            Description = "Retorna todas as metas financeiras do usuário referentes ao mês e ano informados."
        )]
        [ProducesResponseType(typeof(ResponseFinancialGoalsJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetByPeriod (
            [SwaggerParameter("Mês de referência da consulta (1 a 12).")]
            [FromQuery] int month ,

            [SwaggerParameter("Ano de referência da consulta.")]
            [FromQuery] int year ,

            [FromServices] IGetFinancialGoalByPeriodUseCase useCase ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(month , year , cancellationToken);

            if ( response.FinancialGoals.Count != 0 ) {
                return Ok(response);
            }

            return NoContent();
        }

        [HttpGet("{id:long}")]
        [SwaggerOperation(
            Summary = "Obter meta financeira por ID" ,
            Description = "Busca uma meta financeira específica do usuário através do identificador informado."
        )]
        [ProducesResponseType(typeof(ResponseFinancialGoalJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById (
            [SwaggerParameter("Identificador único da meta financeira.")]
            [FromRoute] long id ,

            [FromServices] IGetFinancialGoalByIdUseCase useCase ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(id , cancellationToken);

            return Ok(response);
        }

        [HttpGet("history")]
        [SwaggerOperation(
            Summary = "Consultar histórico de metas financeiras" ,
            Description = "Retorna o histórico paginado das metas financeiras cadastradas pelo usuário (mais recentes primeiro)."
        )]
        [ProducesResponseType(typeof(ResponsePagedJson<ResponseFinancialGoalJson>) , StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistory (
            [FromServices] IGetFinancialGoalsHistoryUseCase useCase ,
            [FromQuery] RequestPaginationJson request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(request , cancellationToken);

            return Ok(response);
        }

        [HttpGet("progress")]
        [PlanHistoryLimit]
        [SwaggerOperation(
            Summary = "Progresso das metas financeiras" ,
            Description = "Compara, para cada meta do período, o valor gasto na categoria com o valor objetivo (percentual usado, saldo restante e status)."
        )]
        [ProducesResponseType(typeof(ResponseFinancialGoalsProgressJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetProgress (
            [SwaggerParameter("Mês de referência da consulta (1 a 12).")]
            [FromQuery] int month ,

            [SwaggerParameter("Ano de referência da consulta.")]
            [FromQuery] int year ,

            [FromServices] IGetFinancialGoalsProgressUseCase useCase ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(month , year , cancellationToken);

            return Ok(response);
        }

        [HttpGet("alerts")]
        [PlanHistoryLimit]
        [SwaggerOperation(
            Summary = "Alertas das metas financeiras" ,
            Description = "Retorna apenas as metas do período que atingiram 80% (Warning) ou ultrapassaram 100% (Exceeded) do valor objetivo."
        )]
        [ProducesResponseType(typeof(ResponseFinancialGoalsAlertsJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAlerts (
            [SwaggerParameter("Mês de referência da consulta (1 a 12).")]
            [FromQuery] int month ,

            [SwaggerParameter("Ano de referência da consulta.")]
            [FromQuery] int year ,

            [FromServices] IGetFinancialGoalsAlertsUseCase useCase ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(month , year , cancellationToken);

            return Ok(response);
        }

        [HttpPut("{id:long}")]
        [SwaggerOperation(
            Summary = "Atualizar uma meta financeira" ,
            Description = "Atualiza o valor objetivo de uma meta financeira existente do usuário."
        )]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update (
            [SwaggerParameter("Identificador único da meta financeira que será atualizada.")]
            [FromRoute] long id ,

            [FromServices] IUpdateFinancialGoalUseCase useCase ,
            [FromBody] RequestUpdateFinancialGoalJson request ,
            CancellationToken cancellationToken )
        {
            await useCase.Execute(id , request , cancellationToken);

            return NoContent();
        }

        [HttpDelete("{id:long}")]
        [SwaggerOperation(
            Summary = "Remover uma meta financeira" ,
            Description = "Realiza a exclusão de uma meta financeira do usuário através do identificador informado."
        )]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete (
            [SwaggerParameter("Identificador único da meta financeira que será removida.")]
            [FromRoute] long id ,

            [FromServices] IDeleteFinancialGoalUseCase useCase ,
            CancellationToken cancellationToken )
        {
            await useCase.Execute(id , cancellationToken);

            return NoContent();
        }

        [HttpPost("copy")]
        [SwaggerOperation(
            Summary = "Copiar metas financeiras entre períodos" ,
            Description = "Copia as metas financeiras de um período de origem para um período de destino ainda sem metas."
        )]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Copy (
            [FromServices] ICopyFinancialGoalsUseCase useCase ,
            [FromBody] RequestCopyFinancialGoalsJson request ,
            CancellationToken cancellationToken )
        {
            await useCase.Execute(request , cancellationToken);

            return NoContent();
        }
    }
}
