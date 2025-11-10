using PagueVeloz.TransactionProcessor.Domain.Entities;

namespace PagueVeloz.TransactionProcessor.Domain.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(string accountId, CancellationToken cancellationToken = default);
    Task<Account?> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default);
    Task<bool> ExistsReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default);
    Task AddAsync(Account account, CancellationToken cancellationToken = default);
    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
    Task<List<Account>> GetAccountsByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
}

