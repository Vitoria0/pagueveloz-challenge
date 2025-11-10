using MediatR;
using PagueVeloz.TransactionProcessor.Application.DTOs;

namespace PagueVeloz.TransactionProcessor.Application.Commands;

public record CreateAccountCommand(CreateAccountDto Dto) : IRequest<AccountDto>;

