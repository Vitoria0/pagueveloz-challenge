namespace PagueVeloz.TransactionProcessor.Domain.Events;

public class AccountActivatedEvent : IDomainEvent
{
    public string AccountId { get; }
    public DateTime OccurredOn { get; }

    public AccountActivatedEvent(string accountId)
    {
        AccountId = accountId;
        OccurredOn = DateTime.UtcNow;
    }
}

