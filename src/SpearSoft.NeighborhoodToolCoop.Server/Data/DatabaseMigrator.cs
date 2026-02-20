using DbUp;
using DbUp.Engine;

namespace SpearSoft.NeighborhoodToolCoop.Server.Data;

/// <summary>
/// Runs DbUp migrations on startup against the configured PostgreSQL database.
///
/// Strategy:
///   - Scripts live in Data/Migrations/ and are embedded in the assembly at build time.
///   - DbUp tracks executed scripts in a `schemaversions` table it creates automatically.
///   - Scripts named V001__, V002__, etc. always run (DDL, schema changes).
///   - Scripts containing "seed_dev_data" only run in the Development environment,
///     so production databases never receive test data.
///   - All scripts are idempotent where possible (IF NOT EXISTS, ON CONFLICT DO NOTHING).
///
/// To add a future migration:
///   1. Create Data/Migrations/V003__describe_change.sql
///   2. The file is automatically embedded and picked up on next startup.
/// </summary>
public static class DatabaseMigrator
{
    public static void RunMigrations(IConfiguration config, IWebHostEnvironment env, ILogger logger)
    {
        var connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is required.");

        // Note: EnsureDatabase requires superuser access to the postgres system database.
        // In our setup (Docker or managed Postgres) the target database is pre-created
        // by the DBA / docker-compose, so we skip this step and connect directly.
        // EnsureDatabase.For.PostgresqlDatabase(connectionString);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                typeof(DatabaseMigrator).Assembly,
                script => ShouldRunScript(script, env.IsDevelopment()))
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            logger.LogError(result.Error, "Database migration failed on script: {Script}",
                result.ErrorScript?.Name);
            throw new Exception("Database migration failed. See logs for details.", result.Error);
        }

        logger.LogInformation("Database migrations applied successfully. Scripts run: {Count}",
            result.Scripts.Count());
    }

    private static bool ShouldRunScript(string scriptName, bool isDevelopment)
    {
        // Dev-only seed scripts are skipped in non-Development environments
        if (scriptName.Contains("seed_dev_data", StringComparison.OrdinalIgnoreCase))
            return isDevelopment;

        return true;
    }
}
