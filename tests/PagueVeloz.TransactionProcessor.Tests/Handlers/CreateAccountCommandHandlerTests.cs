using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PagueVeloz.TransactionProcessor.Application.Commands;
using PagueVeloz.TransactionProcessor.Application.DTOs;
using PagueVeloz.TransactionProcessor.Application.Handlers;
using PagueVeloz.TransactionProcessor.Domain.Repositories;
using Xunit;

namespace PagueVeloz.TransactionProcessor.Tests.Handlers;

public class CreateAccountCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateAccountSuccessfully()
    {
        // Arrange
        var accountRepositoryMock = new Mock<IAccountRepository>();
        var clientRepositoryMock = new Mock<IClientRepository>();
        var loggerMock = new Mock<ILogger<CreateAccountCommandHandler>>();

        var handler = new CreateAccountCommandHandler(
            accountRepositoryMock.Object,
            clientRepositoryMock.Object,
            loggerMock.Object);

        var command = new CreateAccountCommand(new CreateAccountDto
        {
            ClientId = "CLI-001",
            InitialBalance = 1000,
            CreditLimit = 5000
        });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ClientId.Should().Be("CLI-001");
        result.Balance.Should().Be(1000);
        result.CreditLimit.Should().Be(5000);
        accountRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.Account>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

