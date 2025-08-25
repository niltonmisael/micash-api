using MiCash.Communication.Responses;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MiCash.API.Filters
{
    public class ExceptionFilter : IExceptionFilter
    {
        public void OnException ( ExceptionContext context )
        {
            if ( context.Exception is MiCashException ) {

                HandleProjectException ( context );

            } else {

                ThrowUnkownError( context );
            }
            
        }

        private void HandleProjectException (ExceptionContext context)
        {
            var miCash = context.Exception as MiCashException;
            var errorResponse = new ResponseErrorJson(miCash!.GetErrors());

            context.HttpContext.Response.StatusCode = miCash.StatusCode;
            context.Result = new ObjectResult(errorResponse);


        }

        private void ThrowUnkownError ( ExceptionContext context )
        {
            var errorResponse = new ResponseErrorJson(ResourseErrorMessages.UNKNOWN_ERROR);

            context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Result = new ObjectResult(errorResponse);
        }
    }
}
