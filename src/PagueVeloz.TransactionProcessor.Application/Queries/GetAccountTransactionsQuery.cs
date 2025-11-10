using MediatR;
using PagueVeloz.TransactionProcessor.Application.DTOs;

namespace PagueVeloz.TransactionProcessor.Application.Queries;

public record GetAccountTransactionsQuery(string AccountId) : IRequest<List<TransactionDto>>;

