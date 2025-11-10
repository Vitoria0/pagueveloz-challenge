using FluentValidation;
using PagueVeloz.TransactionProcessor.Application.Commands;
using PagueVeloz.TransactionProcessor.Domain.Enums;

namespace PagueVeloz.TransactionProcessor.Application.Validators;

public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.Dto.AccountId)
            .NotEmpty().WithMessage("AccountId é obrigatório");

        RuleFor(x => x.Dto.Amount)
            .GreaterThan(0).WithMessage("Amount deve ser maior que zero");

        RuleFor(x => x.Dto.Currency)
            .NotEmpty().WithMessage("Currency é obrigatório")
            .Length(3).WithMessage("Currency deve ter 3 caracteres");

        RuleFor(x => x.Dto.ReferenceId)
            .NotEmpty().WithMessage("ReferenceId é obrigatório")
            .MaximumLength(100).WithMessage("ReferenceId não pode exceder 100 caracteres");

        RuleFor(x => x.Dto.Operation)
            .IsInEnum().WithMessage("Operation inválida");

        When(x => x.Dto.Operation == TransactionOperation.Transfer, () =>
        {
            RuleFor(x => x.Dto.DestinationAccountId)
                .NotEmpty().WithMessage("DestinationAccountId é obrigatório para transferências")
                .NotEqual(x => x.Dto.AccountId).WithMessage("Conta de origem e destino não podem ser iguais");
        });

        When(x => x.Dto.Operation == TransactionOperation.Reversal, () =>
        {
            RuleFor(x => x.Dto.OriginalReferenceId)
                .NotEmpty().WithMessage("OriginalReferenceId é obrigatório para reversões");
        });
    }
}

