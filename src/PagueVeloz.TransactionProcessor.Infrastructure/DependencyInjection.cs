using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PagueVeloz.TransactionProcessor.Domain.Repositories;
using PagueVeloz.TransactionProcessor.Infrastructure.Data;
using PagueVeloz.TransactionProcessor.Infrastructure.Repositories;
using PagueVeloz.TransactionProcessor.Infrastructure.Services;

namespace PagueVeloz.TransactionProcessor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não encontrada.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            }));

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();

        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>();

        // Registrar DomainEventDispatcher como hosted service
        services.AddSingleton<DomainEventDispatcher>();
        services.AddHostedService(sp => sp.GetRequiredService<DomainEventDispatcher>());

        return services;
    }
}

