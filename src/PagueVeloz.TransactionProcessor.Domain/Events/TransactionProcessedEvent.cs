using PagueVeloz.TransactionProcessor.Domain.Entities;

namespace PagueVeloz.TransactionProcessor.Domain.Events;

public class TransactionProcessedEvent : IDomainEvent
{
    public Transaction Transaction { get; }
    public decimal Balance { get; }
    public decimal ReservedBalance { get; }
    public decimal AvailableBalance { get; }
    public DateTime OccurredOn { get; }

    public TransactionProcessedEvent(Transaction transaction, decimal balance, decimal reservedBalance, decimal availableBalance)
    {
        Transaction = transaction;
        Balance = balance;
        ReservedBalance = reservedBalance;
        AvailableBalance = availableBalance;
        OccurredOn = DateTime.UtcNow;
    }
}

