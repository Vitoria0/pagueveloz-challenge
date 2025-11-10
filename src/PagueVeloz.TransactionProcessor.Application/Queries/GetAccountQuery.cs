using MediatR;
using PagueVeloz.TransactionProcessor.Application.DTOs;

namespace PagueVeloz.TransactionProcessor.Application.Queries;

public record GetAccountQuery(string AccountId) : IRequest<AccountDto?>;

