using FluentValidation;
using GestaoPedidos.Application.DTOs.Produtos;

namespace GestaoPedidos.Application.Validators;

public class CriarProdutoValidator : AbstractValidator<CriarProdutoRequest>
{
    public CriarProdutoValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Descricao).MaximumLength(1000);
        RuleFor(x => x.Preco).GreaterThan(0).WithMessage("Preço deve ser maior que zero.");
        RuleFor(x => x.Estoque).GreaterThanOrEqualTo(0).WithMessage("Estoque não pode ser negativo.");
    }
}
