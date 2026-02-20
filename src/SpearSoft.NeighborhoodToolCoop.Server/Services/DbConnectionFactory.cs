using Npgsql;
using System.Data;

namespace SpearSoft.NeighborhoodToolCoop.Server.Services;

/// <summary>
/// Singleton factory — creates a new NpgsqlConnection per call.
/// Callers are responsible for disposing the returned connection.
/// </summary>
public class DbConnectionFactory(IConfiguration config)
{
    private readonly string _connectionString =
        config.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

    public IDbConnection Create() => new NpgsqlConnection(_connectionString);
}
