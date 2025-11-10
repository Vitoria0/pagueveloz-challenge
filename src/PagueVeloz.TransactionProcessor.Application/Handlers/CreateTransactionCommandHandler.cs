using MediatR;
using Microsoft.Extensions.Logging;
using PagueVeloz.TransactionProcessor.Application.Commands;
using PagueVeloz.TransactionProcessor.Application.DTOs;
using PagueVeloz.TransactionProcessor.Domain.Enums;
using PagueVeloz.TransactionProcessor.Domain.Repositories;
using Polly;
using Polly.Retry;

namespace PagueVeloz.TransactionProcessor.Application.Handlers;

public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, TransactionResponseDto>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<CreateTransactionCommandHandler> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public CreateTransactionCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        ILogger<CreateTransactionCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _logger = logger;

        _retryPolicy = Policy
            .Handle<InvalidOperationException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Tentativa {RetryCount} após {TimeSpan}s. Erro: {Error}",
                        retryCount,
                        timeSpan.TotalSeconds,
                        exception.Message);
                });
    }

    public async Task<TransactionResponseDto> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processando transação {Operation} para conta {AccountId} com reference {ReferenceId}",
            request.Dto.Operation,
            request.Dto.AccountId,
            request.Dto.ReferenceId);

        // Verificar idempotência
        var existingTransaction = await _transactionRepository.GetByReferenceIdAsync(request.Dto.ReferenceId, cancellationToken);
        if (existingTransaction != null)
        {
            _logger.LogInformation("Transação com reference {ReferenceId} já existe. Retornando resultado existente.", request.Dto.ReferenceId);
            
            var account = await _accountRepository.GetByIdAsync(existingTransaction.AccountId, cancellationToken);
            if (account == null)
                throw new InvalidOperationException($"Conta {existingTransaction.AccountId} não encontrada");

            return new TransactionResponseDto
            {
                TransactionId = existingTransaction.TransactionId,
                Status = existingTransaction.Status.ToString().ToLower(),
                Balance = account.Balance,
                ReservedBalance = account.ReservedBalance,
                AvailableBalance = account.AvailableBalance,
                Timestamp = existingTransaction.Timestamp,
                ErrorMessage = existingTransaction.ErrorMessage
            };
        }

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var account = await _accountRepository.GetByIdAsync(request.Dto.AccountId, cancellationToken);
            if (account == null)
                throw new InvalidOperationException($"Conta {request.Dto.AccountId} não encontrada");

            try
            {
                switch (request.Dto.Operation)
                {
                    case TransactionOperation.Credit:
                        account.Credit(request.Dto.Amount, request.Dto.ReferenceId, request.Dto.Currency);
                        break;

                    case TransactionOperation.Debit:
                        account.Debit(request.Dto.Amount, request.Dto.ReferenceId, request.Dto.Currency);
                        break;

                    case TransactionOperation.Reserve:
                        account.Reserve(request.Dto.Amount, request.Dto.ReferenceId, request.Dto.Currency);
                        break;

                    case TransactionOperation.Capture:
                        account.Capture(request.Dto.Amount, request.Dto.ReferenceId, request.Dto.Currency);
                        break;

                    case TransactionOperation.Reversal:
                        if (string.IsNullOrWhiteSpace(request.Dto.OriginalReferenceId))
                            throw new InvalidOperationException("OriginalReferenceId é obrigatório para reversões");
                        account.Reverse(request.Dto.OriginalReferenceId, request.Dto.ReferenceId, request.Dto.Currency);
                        break;

                    case TransactionOperation.Transfer:
                        if (string.IsNullOrWhiteSpace(request.Dto.DestinationAccountId))
                            throw new InvalidOperationException("DestinationAccountId é obrigatório para transferências");
                        
                        var destinationAccount = await _accountRepository.GetByIdAsync(request.Dto.DestinationAccountId, cancellationToken);
                        if (destinationAccount == null)
                            throw new InvalidOperationException($"Conta de destino {request.Dto.DestinationAccountId} não encontrada");
                        
                        account.TransferTo(destinationAccount, request.Dto.Amount, request.Dto.ReferenceId, request.Dto.Currency);
                        await _accountRepository.UpdateAsync(destinationAccount, cancellationToken);
                        break;

                    default:
                        throw new InvalidOperationException($"Operação não suportada: {request.Dto.Operation}");
                }

                var transaction = account.Transactions.Last();
                await _transactionRepository.AddAsync(transaction, cancellationToken);
                await _accountRepository.UpdateAsync(account, cancellationToken);

                _logger.LogInformation(
                    "Transação {TransactionId} processada com sucesso. Status: {Status}",
                    transaction.TransactionId,
                    transaction.Status);

                return new TransactionResponseDto
                {
                    TransactionId = transaction.TransactionId,
                    Status = transaction.Status.ToString().ToLower(),
                    Balance = account.Balance,
                    ReservedBalance = account.ReservedBalance,
                    AvailableBalance = account.AvailableBalance,
                    Timestamp = transaction.Timestamp,
                    ErrorMessage = transaction.ErrorMessage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar transação {ReferenceId}", request.Dto.ReferenceId);
                throw;
            }
        });
    }
}

