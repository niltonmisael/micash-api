using MiCash.Application.UseCases.SavingsGoals.Delete;
using MiCash.Application.UseCases.SavingsGoals.GetAll;
using MiCash.Application.UseCases.SavingsGoals.GetById;
using MiCash.Application.UseCases.SavingsGoals.Movements;
using MiCash.Application.UseCases.SavingsGoals.Register;
using MiCash.Application.UseCases.SavingsGoals.Update;
using MiCash.Communication.Requests.SavingsGoals;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.SavingsGoals;
using MiCash.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MiCash.API.Controllers
{
    [Route("api/savings-goal")]
    [SwaggerTag("Metas de economia (cofrinhos): valor alvo, prazo opcional, depósitos e retiradas. Independentes dos limites de gasto (api/financial-goal).")]
    [ApiController]
    [Authorize]
    public class SavingsGoalController : ControllerBase
    {
        [HttpPost]
        [SwaggerOperation(
            Summary = "Criar meta de economia" ,
            Description = "Cria a meta. Se InitialAmount (\"já guardado\") for maior que zero, vira um depósito com a data de hoje."
        )]
        [ProducesResponseType(typeof(ResponseRegisteredSavingsGoalJson) , StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register (
            [FromServices] IRegisterSavingsGoalUseCase useCase ,
            [FromBody] RequestRegisterSavingsGoalJson request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(request , cancellationToken);

            return Created(string.Empty , response);
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Listar metas de economia" ,
            Description = "Metas em andamento primeiro (por prazo, as sem prazo por último), depois as concluídas."
        )]
        [ProducesResponseType(typeof(ResponseSavingsGoalsJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetAll (
            [FromServices] IGetAllSavingsGoalsUseCase useCase ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(cancellationToken);

            if ( response.SavingsGoals.Count == 0 ) {
                return NoContent();
            }

            return Ok(response);
        }

        [HttpGet("{id:long}")]
        [SwaggerOperation(
            Summary = "Detalhe da meta de economia" ,
            Description = "Valores calculados (guardado, falta, percentual, por mês) e histórico de movimentos, do mais recente para o mais antigo."
        )]
        [ProducesResponseType(typeof(ResponseSavingsGoalJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById (
            [FromRoute] long id ,
            [FromServices] IGetSavingsGoalByIdUseCase useCase ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(id , cancellationToken);

            return Ok(response);
        }

        [HttpPut("{id:long}")]
        [SwaggerOperation(Summary = "Editar meta de economia")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update (
            [FromRoute] long id ,
            [FromServices] IUpdateSavingsGoalUseCase useCase ,
            [FromBody] RequestUpdateSavingsGoalJson request ,
            CancellationToken cancellationToken )
        {
            await useCase.Execute(id , request , cancellationToken);

            return NoContent();
        }

        [HttpDelete("{id:long}")]
        [SwaggerOperation(Summary = "Excluir meta de economia" , Description = "Exclui a meta e todo o seu histórico.")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete (
            [FromRoute] long id ,
            [FromServices] IDeleteSavingsGoalUseCase useCase ,
            CancellationToken cancellationToken )
        {
            await useCase.Execute(id , cancellationToken);

            return NoContent();
        }

        [HttpPost("{id:long}/deposit")]
        [SwaggerOperation(Summary = "Guardar dinheiro (depósito)" , Description = "Depósitos são aceitos mesmo com a meta concluída.")]
        [ProducesResponseType(typeof(ResponseRegisteredSavingsMovementJson) , StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public Task<IActionResult> Deposit (
            [FromRoute] long id ,
            [FromServices] IRegisterSavingsMovementUseCase useCase ,
            [FromBody] RequestSavingsMovementJson request ,
            CancellationToken cancellationToken ) =>
            RegisterMovement(id , SavingsMovementType.Deposit , useCase , request , cancellationToken);

        [HttpPost("{id:long}/withdraw")]
        [SwaggerOperation(Summary = "Retirar dinheiro" , Description = "A retirada não pode passar do valor guardado.")]
        [ProducesResponseType(typeof(ResponseRegisteredSavingsMovementJson) , StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public Task<IActionResult> Withdraw (
            [FromRoute] long id ,
            [FromServices] IRegisterSavingsMovementUseCase useCase ,
            [FromBody] RequestSavingsMovementJson request ,
            CancellationToken cancellationToken ) =>
            RegisterMovement(id , SavingsMovementType.Withdrawal , useCase , request , cancellationToken);

        private async Task<IActionResult> RegisterMovement (
            long id ,
            SavingsMovementType type ,
            IRegisterSavingsMovementUseCase useCase ,
            RequestSavingsMovementJson request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(id , type , request , cancellationToken);

            return Created(string.Empty , response);
        }
    }
}
