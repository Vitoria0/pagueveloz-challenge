namespace PagueVeloz.TransactionProcessor.Domain.Events;

public class AccountDeactivatedEvent : IDomainEvent
{
    public string AccountId { get; }
    public DateTime OccurredOn { get; }

    public AccountDeactivatedEvent(string accountId)
    {
        AccountId = accountId;
        OccurredOn = DateTime.UtcNow;
    }
}

