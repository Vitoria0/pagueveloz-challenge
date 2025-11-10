using PagueVeloz.TransactionProcessor.Domain.Enums;
using PagueVeloz.TransactionProcessor.Domain.Events;
using PagueVeloz.TransactionProcessor.Domain.ValueObjects;

namespace PagueVeloz.TransactionProcessor.Domain.Entities;

public class Account
{
    public string AccountId { get; private set; } = string.Empty;
    public string ClientId { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    public decimal ReservedBalance { get; private set; }
    public decimal CreditLimit { get; private set; }
    public AccountStatus Status { get; private set; }
    public List<Transaction> Transactions { get; private set; } = new();

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Account() { }

    public Account(string accountId, string clientId, decimal initialBalance, decimal creditLimit)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            throw new ArgumentException("AccountId não pode ser vazio", nameof(accountId));
        
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId não pode ser vazio", nameof(clientId));

        if (initialBalance < 0)
            throw new ArgumentException("Saldo inicial não pode ser negativo", nameof(initialBalance));

        if (creditLimit < 0)
            throw new ArgumentException("Limite de crédito não pode ser negativo", nameof(creditLimit));

        AccountId = accountId;
        ClientId = clientId;
        Balance = initialBalance;
        ReservedBalance = 0;
        CreditLimit = creditLimit;
        Status = AccountStatus.Active;
    }

    public decimal AvailableBalance => Balance + CreditLimit;

    public void Credit(decimal amount, string referenceId, string? currency = "BRL")
    {
        if (amount <= 0)
            throw new InvalidOperationException("Valor do crédito deve ser maior que zero");

        if (Status != AccountStatus.Active)
            throw new InvalidOperationException("Conta não está ativa");

        Balance += amount;

        var transaction = new Transaction(
            Guid.NewGuid().ToString(),
            AccountId,
            TransactionOperation.Credit,
            amount,
            currency ?? "BRL",
            referenceId
        );

        Transactions.Add(transaction);
        AddDomainEvent(new TransactionProcessedEvent(transaction, Balance, ReservedBalance, AvailableBalance));
    }

    public void Debit(decimal amount, string referenceId, string? currency = "BRL")
    {
        if (amount <= 0)
            throw new InvalidOperationException("Valor do débito deve ser maior que zero");

        if (Status != AccountStatus.Active)
            throw new InvalidOperationException("Conta não está ativa");

        if (AvailableBalance < amount)
            throw new InvalidOperationException("Saldo disponível insuficiente para débito");

        Balance -= amount;

        var transaction = new Transaction(
            Guid.NewGuid().ToString(),
            AccountId,
            TransactionOperation.Debit,
            amount,
            currency ?? "BRL",
            referenceId
        );

        Transactions.Add(transaction);
        AddDomainEvent(new TransactionProcessedEvent(transaction, Balance, ReservedBalance, AvailableBalance));
    }

    public void Reserve(decimal amount, string referenceId, string? currency = "BRL")
    {
        if (amount <= 0)
            throw new InvalidOperationException("Valor da reserva deve ser maior que zero");

        if (Status != AccountStatus.Active)
            throw new InvalidOperationException("Conta não está ativa");

        if (Balance < amount)
            throw new InvalidOperationException("Saldo disponível insuficiente para reserva");

        Balance -= amount;
        ReservedBalance += amount;

        var transaction = new Transaction(
            Guid.NewGuid().ToString(),
            AccountId,
            TransactionOperation.Reserve,
            amount,
            currency ?? "BRL",
            referenceId
        );

        Transactions.Add(transaction);
        AddDomainEvent(new TransactionProcessedEvent(transaction, Balance, ReservedBalance, AvailableBalance));
    }

    public void Capture(decimal amount, string referenceId, string? currency = "BRL")
    {
        if (amount <= 0)
            throw new InvalidOperationException("Valor da captura deve ser maior que zero");

        if (Status != AccountStatus.Active)
            throw new InvalidOperationException("Conta não está ativa");

        if (ReservedBalance < amount)
            throw new InvalidOperationException("Saldo reservado insuficiente para captura");

        ReservedBalance -= amount;

        var transaction = new Transaction(
            Guid.NewGuid().ToString(),
            AccountId,
            TransactionOperation.Capture,
            amount,
            currency ?? "BRL",
            referenceId
        );

        Transactions.Add(transaction);
        AddDomainEvent(new TransactionProcessedEvent(transaction, Balance, ReservedBalance, AvailableBalance));
    }

    public void Reverse(string originalReferenceId, string newReferenceId, string? currency = "BRL")
    {
        var originalTransaction = Transactions
            .FirstOrDefault(t => t.ReferenceId == originalReferenceId && t.Status == TransactionStatus.Success);

        if (originalTransaction == null)
            throw new InvalidOperationException($"Transação original não encontrada: {originalReferenceId}");

        if (Status != AccountStatus.Active)
            throw new InvalidOperationException("Conta não está ativa");

        decimal amount = originalTransaction.Amount;

        switch (originalTransaction.Operation)
        {
            case TransactionOperation.Credit:
                if (Balance < amount)
                    throw new InvalidOperationException("Saldo insuficiente para reverter crédito");
                Balance -= amount;
                break;

            case TransactionOperation.Debit:
                Balance += amount;
                break;

            case TransactionOperation.Reserve:
                Balance += amount;
                ReservedBalance -= amount;
                break;

            case TransactionOperation.Capture:
                ReservedBalance += amount;
                break;

            case TransactionOperation.Transfer:
                // Transfer reversals são tratados separadamente
                break;

            default:
                throw new InvalidOperationException($"Operação não pode ser revertida: {originalTransaction.Operation}");
        }

        var transaction = new Transaction(
            Guid.NewGuid().ToString(),
            AccountId,
            TransactionOperation.Reversal,
            amount,
            currency ?? "BRL",
            newReferenceId
        );

        Transactions.Add(transaction);
        AddDomainEvent(new TransactionProcessedEvent(transaction, Balance, ReservedBalance, AvailableBalance));
    }

    public void TransferTo(Account destinationAccount, decimal amount, string referenceId, string? currency = "BRL")
    {
        if (destinationAccount == null)
            throw new ArgumentNullException(nameof(destinationAccount));

        if (amount <= 0)
            throw new InvalidOperationException("Valor da transferência deve ser maior que zero");

        if (Status != AccountStatus.Active || destinationAccount.Status != AccountStatus.Active)
            throw new InvalidOperationException("Ambas as contas devem estar ativas");

        if (AvailableBalance < amount)
            throw new InvalidOperationException("Saldo disponível insuficiente para transferência");

        Balance -= amount;
        destinationAccount.Balance += amount;

        var sourceTransaction = new Transaction(
            Guid.NewGuid().ToString(),
            AccountId,
            TransactionOperation.Transfer,
            amount,
            currency ?? "BRL",
            referenceId
        );

        var destinationTransaction = new Transaction(
            Guid.NewGuid().ToString(),
            destinationAccount.AccountId,
            TransactionOperation.Transfer,
            amount,
            currency ?? "BRL",
            referenceId
        );

        Transactions.Add(sourceTransaction);
        destinationAccount.Transactions.Add(destinationTransaction);

        AddDomainEvent(new TransactionProcessedEvent(sourceTransaction, Balance, ReservedBalance, AvailableBalance));
        destinationAccount.AddDomainEvent(new TransactionProcessedEvent(destinationTransaction, destinationAccount.Balance, destinationAccount.ReservedBalance, destinationAccount.AvailableBalance));
    }

    public void Block()
    {
        Status = AccountStatus.Blocked;
        AddDomainEvent(new AccountBlockedEvent(AccountId));
    }

    public void Activate()
    {
        Status = AccountStatus.Active;
        AddDomainEvent(new AccountActivatedEvent(AccountId));
    }

    public void Deactivate()
    {
        Status = AccountStatus.Inactive;
        AddDomainEvent(new AccountDeactivatedEvent(AccountId));
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}

