using Microsoft.OpenApi;

namespace MiCash.API.Extensions;

public static class SwaggerExtension
{
    public static IServiceCollection AddSwaggerDocumentation ( this IServiceCollection services )
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApi();
        services.AddSwaggerGen(config => {
            config.EnableAnnotations();

            config.AddSecurityDefinition("Bearer" , new OpenApiSecurityScheme
            {
                Name = "Authorization" ,
                Description = """
                    JWT Authorization header using Bearer scheme.
                    Enter 'Bearer' [space] and then your token.
                    Example: 'Bearer 1234abcdf'
                    """ ,
                In = ParameterLocation.Header ,
                Scheme = "Bearer" ,
                Type = SecuritySchemeType.Http ,
                BearerFormat = "JWT"
            });

            config.AddSecurityRequirement(document =>
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer" , document)] = []
                });
        });

        return services;
    }

    public static WebApplication UseSwaggerDocumentation ( this WebApplication app )
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI();

        return app;
    }
}
