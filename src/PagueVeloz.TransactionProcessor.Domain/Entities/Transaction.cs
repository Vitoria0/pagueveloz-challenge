using PagueVeloz.TransactionProcessor.Domain.Enums;

namespace PagueVeloz.TransactionProcessor.Domain.Entities;

public class Transaction
{
    public string TransactionId { get; private set; } = string.Empty;
    public string AccountId { get; private set; } = string.Empty;
    public TransactionOperation Operation { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "BRL";
    public string ReferenceId { get; private set; } = string.Empty;
    public TransactionStatus Status { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string? ErrorMessage { get; private set; }

    private Transaction() { }

    public Transaction(
        string transactionId,
        string accountId,
        TransactionOperation operation,
        decimal amount,
        string currency,
        string referenceId)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            throw new ArgumentException("TransactionId não pode ser vazio", nameof(transactionId));
        
        if (string.IsNullOrWhiteSpace(accountId))
            throw new ArgumentException("AccountId não pode ser vazio", nameof(accountId));

        if (amount <= 0)
            throw new ArgumentException("Amount deve ser maior que zero", nameof(amount));

        if (string.IsNullOrWhiteSpace(referenceId))
            throw new ArgumentException("ReferenceId não pode ser vazio", nameof(referenceId));

        TransactionId = transactionId;
        AccountId = accountId;
        Operation = operation;
        Amount = amount;
        Currency = currency;
        ReferenceId = referenceId;
        Status = TransactionStatus.Success;
        Timestamp = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = TransactionStatus.Failed;
        ErrorMessage = errorMessage;
    }

    public void MarkAsPending()
    {
        Status = TransactionStatus.Pending;
    }
}

