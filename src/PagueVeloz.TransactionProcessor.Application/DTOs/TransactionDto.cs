using PagueVeloz.TransactionProcessor.Domain.Enums;

namespace PagueVeloz.TransactionProcessor.Application.DTOs;

public record TransactionDto
{
    public string TransactionId { get; init; } = string.Empty;
    public string AccountId { get; init; } = string.Empty;
    public TransactionOperation Operation { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string ReferenceId { get; init; } = string.Empty;
    public TransactionStatus Status { get; init; }
    public DateTime Timestamp { get; init; }
    public string? ErrorMessage { get; init; }
}

