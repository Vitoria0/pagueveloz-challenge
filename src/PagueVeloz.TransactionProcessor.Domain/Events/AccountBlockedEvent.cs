namespace PagueVeloz.TransactionProcessor.Domain.Events;

public class AccountBlockedEvent : IDomainEvent
{
    public string AccountId { get; }
    public DateTime OccurredOn { get; }

    public AccountBlockedEvent(string accountId)
    {
        AccountId = accountId;
        OccurredOn = DateTime.UtcNow;
    }
}

