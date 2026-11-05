namespace MiCash.API.Middleware
{
    public static class CultureMiddlewareExtension
    {
        public static IApplicationBuilder UseCulture ( this IApplicationBuilder app )
        {
            return app.UseMiddleware<CultureMiddleware>();
        }
    }
}
