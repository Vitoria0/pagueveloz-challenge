namespace PagueVeloz.TransactionProcessor.Domain.Entities;

public class Client
{
    public string ClientId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public List<Account> Accounts { get; private set; } = new();

    private Client() { }

    public Client(string clientId, string name)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId não pode ser vazio", nameof(clientId));
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name não pode ser vazio", nameof(name));

        ClientId = clientId;
        Name = name;
    }

    public void AddAccount(Account account)
    {
        if (account == null)
            throw new ArgumentNullException(nameof(account));

        Accounts.Add(account);
    }
}

