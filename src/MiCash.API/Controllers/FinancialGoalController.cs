using MiCash.Application.UseCases.FinancialGols.Copy;
using MiCash.Application.UseCases.FinancialGols.Delete;
using MiCash.Application.UseCases.FinancialGols.GetById;
using MiCash.Application.UseCases.FinancialGols.GetByPeriod;
using MiCash.Application.UseCases.FinancialGols.History;
using MiCash.Application.UseCases.FinancialGols.Register;
using MiCash.Application.UseCases.FinancialGols.Update;
using MiCash.Communication.Pagination;
using MiCash.Communication.Requests.FinancialGoals;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.FinancialGoals;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel;

namespace MiCash.API.Controllers
{
    [Route("api/financial-goal")]
    [SwaggerTag("Endpoints responsáveis pelo gerenciamento das metas financeiras do usuário.")]
    [ApiController]
    public class FinancialGoalController : ControllerBase
    {
        [HttpPost]
        [SwaggerOperation(
            Summary = "Criar uma meta financeira" ,
            Description = "Cria uma nova meta financeira para o usuário autenticado, permitindo definir um valor objetivo e um período de acompanhamento."
        )]
        [ProducesResponseType(typeof(ResponseRegisteredFinancialGoalJson) , StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register (
            [FromServices] IRegisterFinancialGoalUseCase useCase ,
            [FromBody] RequestRegisterFinancialGoal request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(request , cancellationToken);

            return Created(string.Empty , response);
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Listar metas financeiras por período" ,
            Description = "Retorna todas as metas financeiras do usuário referentes ao mês e ano informados."
        )]
        [ProducesResponseType(typeof(ResponseFinancialGoalsJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetByPeriod (
            [FromQuery] int month ,
            [SwaggerParameter("Mês de referência da consulta (1 a 12).")]

            [FromQuery] int year ,
            [SwaggerParameter("Ano de referência da consulta.")]

            [FromServices] IGetFinancialGoalByPeriodUseCase useCase ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(month , year , cancellationToken);

            if ( response.FinancialGoals.Count != 0 ) {

                return Ok(response);
            }
            return NoContent();
        }

        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Obter meta financeira por ID" ,
            Description = "Busca uma meta financeira específica através do identificador informado."
        )]
        [ProducesResponseType(typeof(ResponseFinancialGoalsJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]

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
            Description = "Retorna o histórico paginado das metas financeiras cadastradas pelo usuário."
        )]
        [ProducesResponseType(typeof(ResponseFinancialGoalsJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetHistory (
            [FromServices] IGetFinancialGoalsHistoryUseCase useCase ,
            [FromQuery] RequestPaginationJson request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(request , cancellationToken);

            return Ok(response);
        }

        [HttpDelete]
        [SwaggerOperation(
            Summary = "Remover uma meta financeira" ,
            Description = "Realiza a exclusão de uma meta financeira através do identificador informado."
        )]
        [Route("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete (
            [FromServices] IDeleteFinancialGoalUseCase useCase ,

            [SwaggerParameter("Identificador único da meta financeira que será removida.")]
            long id ,

            CancellationToken cancellationToken )
        {
            await useCase.Execute(id , cancellationToken);

            return NoContent();
        }

        [HttpPut("{id}")]
        [SwaggerOperation(
            Summary = "Atualizar uma meta financeira" ,
            Description = "Atualiza as informações de uma meta financeira existente através do identificador informado."
        )]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update (
            [FromServices] IUpdateFinancialGoalUseCase useCase ,

            [SwaggerParameter("Identificador único da meta financeira que será atualizada.")]
            long id ,

            [FromBody] RequestUpdateFinancialGoalJson request ,
            CancellationToken cancellationToken )
        {
            await useCase.Execute(id , request , cancellationToken);

            return NoContent();
        }

        [HttpPost("copy")]
        [SwaggerOperation(
            Summary = "Copiar metas financeiras entre períodos" ,
            Description = "Copia as metas financeiras de um período de origem para um novo período informado pelo usuário."
        )]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
        
        public async Task<IActionResult> Copy (
            [FromServices] ICopyFinancialGoalsUseCase useCase ,
            [FromBody] RequestCopyFinancialGoalsJson request ,
            CancellationToken cancellationToken )
        {
            await useCase.Execute(
                request ,
                cancellationToken);

            return NoContent();
        }
    }
}
