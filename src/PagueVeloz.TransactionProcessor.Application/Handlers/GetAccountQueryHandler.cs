using MediatR;
using Microsoft.Extensions.Logging;
using PagueVeloz.TransactionProcessor.Application.DTOs;
using PagueVeloz.TransactionProcessor.Application.Queries;
using PagueVeloz.TransactionProcessor.Domain.Repositories;

namespace PagueVeloz.TransactionProcessor.Application.Handlers;

public class GetAccountQueryHandler : IRequestHandler<GetAccountQuery, AccountDto?>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<GetAccountQueryHandler> _logger;

    public GetAccountQueryHandler(
        IAccountRepository accountRepository,
        ILogger<GetAccountQueryHandler> logger)
    {
        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task<AccountDto?> Handle(GetAccountQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Buscando conta {AccountId}", request.AccountId);

        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account == null)
        {
            _logger.LogWarning("Conta {AccountId} não encontrada", request.AccountId);
            return null;
        }

        return new AccountDto
        {
            AccountId = account.AccountId,
            ClientId = account.ClientId,
            Balance = account.Balance,
            ReservedBalance = account.ReservedBalance,
            CreditLimit = account.CreditLimit,
            AvailableBalance = account.AvailableBalance,
            Status = account.Status
        };
    }
}

