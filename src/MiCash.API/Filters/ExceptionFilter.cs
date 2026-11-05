using MiCash.Communication.Responses;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MiCash.API.Filters
{
    public class ExceptionFilter ( ILogger<ExceptionFilter> logger ) : IExceptionFilter
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
            var errorResponse = new ResponseErrorJson(miCash!.GetErrors())
            {
                ErrorCode = miCash.ErrorCode ,
                RetryAt = miCash.RetryAt
            };

            context.HttpContext.Response.StatusCode = miCash.StatusCode;
            context.Result = new ObjectResult(errorResponse);


        }

        private void ThrowUnkownError ( ExceptionContext context )
        {
            // The client only gets the generic message; the real error (with stack trace) goes to the log (Loki).
            logger.LogError(context.Exception ,
                "Unhandled exception on {Method} {Path}" ,
                context.HttpContext.Request.Method ,
                context.HttpContext.Request.Path);

            var errorResponse = new ResponseErrorJson(ResourceErrorMessages.UNKNOWN_ERROR);

            context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Result = new ObjectResult(errorResponse);
        }
    }
}
