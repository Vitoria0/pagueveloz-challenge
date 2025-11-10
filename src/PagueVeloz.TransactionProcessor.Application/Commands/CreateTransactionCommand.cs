using MediatR;
using PagueVeloz.TransactionProcessor.Application.DTOs;

namespace PagueVeloz.TransactionProcessor.Application.Commands;

public record CreateTransactionCommand(CreateTransactionDto Dto) : IRequest<TransactionResponseDto>;

