using FluentAssertions;
using PagueVeloz.TransactionProcessor.Domain.Entities;
using PagueVeloz.TransactionProcessor.Domain.Enums;
using Xunit;

namespace PagueVeloz.TransactionProcessor.Tests.Unit;

public class AccountTests
{
    [Fact]
    public void Credit_ShouldIncreaseBalance()
    {
        // Arrange
        var account = new Account("ACC-001", "CLI-001", 1000, 5000);
        var initialBalance = account.Balance;

        // Act
        account.Credit(500, "REF-001");

        // Assert
        account.Balance.Should().Be(initialBalance + 500);
        account.Transactions.Should().HaveCount(1);
        account.Transactions.First().Operation.Should().Be(TransactionOperation.Credit);
    }

    [Fact]
    public void Debit_ShouldDecreaseBalance()
    {
        // Arrange
        var account = new Account("ACC-001", "CLI-001", 1000, 5000);
        var initialBalance = account.Balance;

        // Act
        account.Debit(300, "REF-002");

        // Assert
        account.Balance.Should().Be(initialBalance - 300);
        account.Transactions.Should().HaveCount(1);
    }

    [Fact]
    public void Debit_ShouldThrowException_WhenInsufficientBalance()
    {
        // Arrange
        var account = new Account("ACC-001", "CLI-001", 100, 0);

        // Act & Assert
        var action = () => account.Debit(500, "REF-003");
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*insuficiente*");
    }

    [Fact]
    public void Reserve_ShouldMoveBalanceToReserved()
    {
        // Arrange
        var account = new Account("ACC-001", "CLI-001", 1000, 0);
        var initialBalance = account.Balance;
        var initialReserved = account.ReservedBalance;

        // Act
        account.Reserve(300, "REF-004");

        // Assert
        account.Balance.Should().Be(initialBalance - 300);
        account.ReservedBalance.Should().Be(initialReserved + 300);
    }

    [Fact]
    public void Capture_ShouldRemoveFromReserved()
    {
        // Arrange
        var account = new Account("ACC-001", "CLI-001", 1000, 0);
        account.Reserve(300, "REF-005");
        var initialReserved = account.ReservedBalance;

        // Act
        account.Capture(200, "REF-006");

        // Assert
        account.ReservedBalance.Should().Be(initialReserved - 200);
    }

    [Fact]
    public void Transfer_ShouldMoveBalanceBetweenAccounts()
    {
        // Arrange
        var sourceAccount = new Account("ACC-001", "CLI-001", 1000, 0);
        var destinationAccount = new Account("ACC-002", "CLI-002", 500, 0);
        var sourceInitialBalance = sourceAccount.Balance;
        var destInitialBalance = destinationAccount.Balance;

        // Act
        sourceAccount.TransferTo(destinationAccount, 300, "REF-007");

        // Assert
        sourceAccount.Balance.Should().Be(sourceInitialBalance - 300);
        destinationAccount.Balance.Should().Be(destInitialBalance + 300);
    }

    [Fact]
    public void Reverse_ShouldRevertCredit()
    {
        // Arrange
        var account = new Account("ACC-001", "CLI-001", 1000, 0);
        account.Credit(500, "REF-008");
        var balanceAfterCredit = account.Balance;

        // Act
        account.Reverse("REF-008", "REF-009");

        // Assert
        account.Balance.Should().Be(balanceAfterCredit - 500);
    }
}

