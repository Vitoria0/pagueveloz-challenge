using Microsoft.EntityFrameworkCore;
using PagueVeloz.TransactionProcessor.Domain.Entities;
using PagueVeloz.TransactionProcessor.Infrastructure.Data.Configurations;

namespace PagueVeloz.TransactionProcessor.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<Client> Clients { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new AccountConfiguration());
        modelBuilder.ApplyConfiguration(new ClientConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());

        // Configurar concorrência otimista
        modelBuilder.Entity<Account>()
            .Property(a => a.Balance)
            .IsConcurrencyToken();

        modelBuilder.Entity<Account>()
            .Property(a => a.ReservedBalance)
            .IsConcurrencyToken();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);

        // Limpar eventos após salvar
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is Account && (e.State == EntityState.Added || e.State == EntityState.Modified))
            .Select(e => e.Entity as Account)
            .Where(a => a != null)
            .ToList();

        foreach (var account in entries)
        {
            account?.ClearDomainEvents();
        }

        return result;
    }
}

