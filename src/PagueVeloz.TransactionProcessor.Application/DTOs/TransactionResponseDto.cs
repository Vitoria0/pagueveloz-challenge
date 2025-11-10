using PagueVeloz.TransactionProcessor.Domain.Enums;

namespace PagueVeloz.TransactionProcessor.Application.DTOs;

public record TransactionResponseDto
{
    public string TransactionId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public decimal ReservedBalance { get; init; }
    public decimal AvailableBalance { get; init; }
    public DateTime Timestamp { get; init; }
    public string? ErrorMessage { get; init; }
}

