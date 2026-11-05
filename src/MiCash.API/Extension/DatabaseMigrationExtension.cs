using MiCash.Infrastructure.Migrations;

namespace MiCash.API.Extensions;

public static class DatabaseMigrationExtension
{
    private const int MAX_ATTEMPTS = 5;

    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    /// <summary>Applies the migrations, retrying while the database is not reachable yet (e.g. container start-up).</summary>
    public static async Task MigrateDatabaseAsync ( this WebApplication app )
    {
        await using var scope = app.Services.CreateAsyncScope();
        var serviceProvider = scope.ServiceProvider;

        for ( int attempt = 1; attempt <= MAX_ATTEMPTS; attempt++ ) {
            try {
                Console.WriteLine($"Tentando conectar no banco (tentativa {attempt})...");

                await DataBaseMigration.MigrateDatabase(serviceProvider);

                Console.WriteLine("Banco conectado e migrations aplicadas!");
                break;
            } catch ( System.Exception ex ) {
                Console.WriteLine($"Erro ao conectar: {ex.Message}");

                if ( attempt == MAX_ATTEMPTS )
                    throw;

                await Task.Delay(RetryDelay);
            }
        }
    }
}
