using MediatR;
using Microsoft.Extensions.Logging;
using PagueVeloz.TransactionProcessor.Application.DTOs;
using PagueVeloz.TransactionProcessor.Application.Queries;
using PagueVeloz.TransactionProcessor.Domain.Repositories;

namespace PagueVeloz.TransactionProcessor.Application.Handlers;

public class GetAccountTransactionsQueryHandler : IRequestHandler<GetAccountTransactionsQuery, List<TransactionDto>>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<GetAccountTransactionsQueryHandler> _logger;

    public GetAccountTransactionsQueryHandler(
        ITransactionRepository transactionRepository,
        ILogger<GetAccountTransactionsQueryHandler> logger)
    {
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    public async Task<List<TransactionDto>> Handle(GetAccountTransactionsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Buscando transações da conta {AccountId}", request.AccountId);

        var transactions = await _transactionRepository.GetByAccountIdAsync(request.AccountId, cancellationToken);

        return transactions.Select(t => new TransactionDto
        {
            TransactionId = t.TransactionId,
            AccountId = t.AccountId,
            Operation = t.Operation,
            Amount = t.Amount,
            Currency = t.Currency,
            ReferenceId = t.ReferenceId,
            Status = t.Status,
            Timestamp = t.Timestamp,
            ErrorMessage = t.ErrorMessage
        }).ToList();
    }
}

