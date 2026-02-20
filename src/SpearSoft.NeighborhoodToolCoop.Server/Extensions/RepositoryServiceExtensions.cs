using SpearSoft.NeighborhoodToolCoop.Server.Repositories;
using SpearSoft.NeighborhoodToolCoop.Server.Repositories.Interfaces;

namespace SpearSoft.NeighborhoodToolCoop.Server.Extensions;

public static class RepositoryServiceExtensions
{
    /// <summary>
    /// Registers all repository interfaces with their concrete implementations.
    ///
    /// Scoped lifetime matches TenantContext (also scoped), ensuring every request
    /// gets a consistent tenant identity across all repositories it touches.
    ///
    /// AuditLogRepository and TenantRepository are also scoped for simplicity,
    /// even though they don't inherit RepositoryBase.
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ITenantRepository,       TenantRepository>();
        services.AddScoped<IUserRepository,         UserRepository>();
        services.AddScoped<ITenantMemberRepository, TenantMemberRepository>();
        services.AddScoped<ILocationRepository,     LocationRepository>();
        services.AddScoped<IToolRepository,         ToolRepository>();
        services.AddScoped<IToolAttributeRepository,ToolAttributeRepository>();
        services.AddScoped<ILoanRepository,         LoanRepository>();
        services.AddScoped<IReservationRepository,  ReservationRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IAuditLogRepository,     AuditLogRepository>();

        return services;
    }
}
