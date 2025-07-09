using MiCash.API.Filters;
using MiCash.API.Middleware;
using MiCash.Infrastructure;
using MiCash.Application;
using MiCash.Infrastructure.DataAccess;
using MiCash.Infrastructure.Migrations;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddMvc(options => options.Filters.Add(typeof(ExceptionFilter)));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CultureMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await MigrateDataBase();

app.Run();

async Task MigrateDataBase ()
{
    await using var scope = app.Services.CreateAsyncScope();

    await DataBaseMigration.MigrationDataBase(scope.ServiceProvider);

}
