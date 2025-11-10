using Microsoft.EntityFrameworkCore;
using PagueVeloz.TransactionProcessor.Domain.Entities;
using PagueVeloz.TransactionProcessor.Domain.Repositories;
using PagueVeloz.TransactionProcessor.Infrastructure.Data;

namespace PagueVeloz.TransactionProcessor.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly ApplicationDbContext _context;

    public AccountRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.AccountId == accountId, cancellationToken);
    }

    public async Task<Account?> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.ReferenceId == referenceId, cancellationToken);

        if (transaction == null)
            return null;

        return await GetByIdAsync(transaction.AccountId, cancellationToken);
    }

    public async Task<bool> ExistsReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AnyAsync(t => t.ReferenceId == referenceId, cancellationToken);
    }

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        await _context.Accounts.AddAsync(account, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        _context.Accounts.Update(account);
        
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Retry com lock pessimista se necessário
            throw new InvalidOperationException("Conflito de concorrência detectado. Tente novamente.");
        }
    }

    public async Task<List<Account>> GetAccountsByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .Where(a => a.ClientId == clientId)
            .Include(a => a.Transactions)
            .ToListAsync(cancellationToken);
    }
}

