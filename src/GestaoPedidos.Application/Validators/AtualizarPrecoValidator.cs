using FluentValidation;
using GestaoPedidos.Application.DTOs.Produtos;

namespace GestaoPedidos.Application.Validators;

public class AtualizarPrecoValidator : AbstractValidator<AtualizarPrecoRequest>
{
    public AtualizarPrecoValidator()
    {
        RuleFor(x => x.Preco).GreaterThan(0).WithMessage("O preço deve ser maior que zero.");
    }
}
