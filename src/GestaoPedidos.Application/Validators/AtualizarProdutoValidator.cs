using FluentValidation;
using GestaoPedidos.Application.DTOs.Produtos;

namespace GestaoPedidos.Application.Validators;

public class AtualizarProdutoValidator : AbstractValidator<AtualizarProdutoRequest>
{
    public AtualizarProdutoValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Descricao).MaximumLength(1000);
    }
}
