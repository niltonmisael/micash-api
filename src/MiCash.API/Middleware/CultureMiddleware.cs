using System.Globalization;

namespace MiCash.API.Middleware
{
    public class CultureMiddleware
    {
        private readonly RequestDelegate _next;

        public CultureMiddleware (RequestDelegate next)
        {
            _next = next;
        }
        public async Task Invoke(HttpContext context )
        {
            var supportLanguages = CultureInfo.GetCultures(CultureTypes.AllCultures).ToList();

            var requestedCulture = context.Request.Headers.AcceptLanguage.FirstOrDefault();


            var cultereInfo = new CultureInfo("pt-BR");

            if ( string.IsNullOrWhiteSpace(requestedCulture) == false && supportLanguages.Exists(language => language.Name.Equals(requestedCulture))) {

                cultereInfo = new CultureInfo (requestedCulture);
            }

            CultureInfo.CurrentCulture = cultereInfo;
            CultureInfo.CurrentUICulture = cultereInfo;

            await _next(context);
                
        }
    }
}
