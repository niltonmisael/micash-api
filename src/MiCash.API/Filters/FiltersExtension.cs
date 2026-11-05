namespace MiCash.API.Filters
{
    public static class FiltersExtension
    {
        public static IServiceCollection AddApiFilters ( this IServiceCollection services )
        {
            services.AddMvc(options => {
                options.Filters.Add(typeof(ExceptionFilter));
                // Global, before the subscription/plan filters: an unconfirmed e-mail is answered first.
                options.Filters.Add(typeof(EmailVerifiedFilter));
            });

            services.AddScoped<RequireActiveSubscriptionFilter>();

            return services;
        }
    }
}
