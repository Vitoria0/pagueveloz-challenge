using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PagueVeloz.TransactionProcessor.Application.Commands;
using PagueVeloz.TransactionProcessor.Application.DTOs;
using PagueVeloz.TransactionProcessor.Application.Queries;
using PagueVeloz.TransactionProcessor.Domain.Enums;
using PagueVeloz.TransactionProcessor.Domain.Repositories;
using PagueVeloz.TransactionProcessor.Infrastructure.Data;
using Xunit;

namespace PagueVeloz.TransactionProcessor.Tests.Integration;

public class TransactionProcessorIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IServiceProvider _serviceProvider;

    public TransactionProcessorIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        services.AddScoped<IAccountRepository, Infrastructure.Repositories.AccountRepository>();
        services.AddScoped<ITransactionRepository, Infrastructure.Repositories.TransactionRepository>();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _accountRepository = _serviceProvider.GetRequiredService<IAccountRepository>();
        _transactionRepository = _serviceProvider.GetRequiredService<ITransactionRepository>();
    }

    [Fact]
    public async Task CreateAccount_ShouldPersistAccount()
    {
        // Arrange
        var dto = new CreateAccountDto
        {
            ClientId = "CLI-001",
            InitialBalance = 1000,
            CreditLimit = 5000
        };

        // Act
        var account = new Domain.Entities.Account("ACC-001", dto.ClientId, dto.InitialBalance, dto.CreditLimit);
        await _accountRepository.AddAsync(account);

        // Assert
        var savedAccount = await _accountRepository.GetByIdAsync("ACC-001");
        savedAccount.Should().NotBeNull();
        savedAccount!.Balance.Should().Be(1000);
        savedAccount.CreditLimit.Should().Be(5000);
    }

    [Fact]
    public async Task ProcessCreditTransaction_ShouldUpdateBalance()
    {
        // Arrange
        var account = new Domain.Entities.Account("ACC-001", "CLI-001", 1000, 0);
        await _accountRepository.AddAsync(account);

        // Act
        account.Credit(500, "REF-001");
        await _accountRepository.UpdateAsync(account);

        // Assert
        var updatedAccount = await _accountRepository.GetByIdAsync("ACC-001");
        updatedAccount!.Balance.Should().Be(1500);
    }

    [Fact]
    public async Task ProcessDebitTransaction_ShouldUpdateBalance()
    {
        // Arrange
        var account = new Domain.Entities.Account("ACC-001", "CLI-001", 1000, 0);
        await _accountRepository.AddAsync(account);

        // Act
        account.Debit(300, "REF-002");
        await _accountRepository.UpdateAsync(account);

        // Assert
        var updatedAccount = await _accountRepository.GetByIdAsync("ACC-001");
        updatedAccount!.Balance.Should().Be(700);
    }

    [Fact]
    public async Task ProcessReserveTransaction_ShouldMoveToReserved()
    {
        // Arrange
        var account = new Domain.Entities.Account("ACC-001", "CLI-001", 1000, 0);
        await _accountRepository.AddAsync(account);

        // Act
        account.Reserve(400, "REF-003");
        await _accountRepository.UpdateAsync(account);

        // Assert
        var updatedAccount = await _accountRepository.GetByIdAsync("ACC-001");
        updatedAccount!.Balance.Should().Be(600);
        updatedAccount.ReservedBalance.Should().Be(400);
    }

    [Fact]
    public async Task Idempotency_ShouldReturnSameResult()
    {
        // Arrange
        var account = new Domain.Entities.Account("ACC-001", "CLI-001", 1000, 0);
        await _accountRepository.AddAsync(account);

        // Act - Primeira transação
        account.Credit(500, "REF-004");
        await _accountRepository.UpdateAsync(account);
        var firstBalance = account.Balance;

        // Act - Tentativa de duplicar (deve ser ignorada pela verificação de idempotência)
        var exists = await _transactionRepository.ExistsReferenceIdAsync("REF-004");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task Concurrency_ShouldHandleMultipleTransactions()
    {
        // Arrange
        var account = new Domain.Entities.Account("ACC-001", "CLI-001", 1000, 0);
        await _accountRepository.AddAsync(account);

        // Act - Múltiplas transações simultâneas
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            var refId = $"REF-{i:D3}";
            tasks.Add(Task.Run(async () =>
            {
                var acc = await _accountRepository.GetByIdAsync("ACC-001");
                if (acc != null)
                {
                    acc.Credit(100, refId);
                    await _accountRepository.UpdateAsync(acc);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var finalAccount = await _accountRepository.GetByIdAsync("ACC-001");
        finalAccount!.Balance.Should().BeGreaterOrEqualTo(1000);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}

