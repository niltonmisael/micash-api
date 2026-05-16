using MiCash.API.Filters;
using MiCash.API.Middleware;
using MiCash.API.Token;
using MiCash.Domain.Security.Tokens;
using MiCash.Infrastructure;
using MiCash.Infrastructure.Extensions;
using MiCash.Infrastructure.Migrations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;


var builder = WebApplication.CreateBuilder(args);
Console.WriteLine($"ENV: {builder.Environment.EnvironmentName}");
builder.Services.AddControllers();
builder.Services.AddRouting(config => {
    config.LowercaseUrls = true;
});
builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(config => {
    config.AddSecurityDefinition("Bearer" , new OpenApiSecurityScheme
    {
        Name = "Authorization" ,
        Description = @"JWT Authorization header using Bearer scheme. 
                      Enter 'Bearer' [space] and then your token in the text input below.
                      Example: 'Bearer 1234abcdf'",
        In = ParameterLocation.Header,
        Scheme = "Bearer",
        Type = SecuritySchemeType.ApiKey
    });

    config.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});
builder.Services.AddMvc(options => options.Filters.Add(typeof(ExceptionFilter)));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddCors(options => {
    options.AddPolicy("AllowFrontend" , policy => {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddScoped<ITokenProvider , HttpContextTokenValue>();

builder.Services.AddHttpContextAccessor();

var signingKey = builder.Configuration.GetValue<string>("Settings:Jwt:SingingKey");

builder.Services.AddAuthentication(config => {
    config.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    config.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(config => {
    config.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false ,
        ValidateAudience = false ,
        ClockSkew = new TimeSpan(0),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey!))
    };
});

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CultureMiddleware>();
app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

if(builder.Configuration.IsTestEnvironment() == false ) {
     
    await MigrateDataBase();

}


app.Run();

async Task MigrateDataBase ()
{
    await using var scope = app.Services.CreateAsyncScope();
    var serviceProvider = scope.ServiceProvider;

    for ( int i = 1; i <= 5; i++ ) {
        try {
            Console.WriteLine($"Tentando conectar no banco (tentativa {i})...");

            await DataBaseMigration.MigrateDatabase(serviceProvider);

            Console.WriteLine("Banco conectado e migrations aplicadas!");
            break;
        } catch ( Exception ex ) {
            Console.WriteLine($"Erro ao conectar: {ex.Message}");

            if ( i == 5 )
                throw;

            await Task.Delay(3000);
        }
    }
}

public partial class Program { }