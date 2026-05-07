using FluentValidation;
using GestaoPedidos.Application.DTOs.Pedidos;

namespace GestaoPedidos.Application.Validators;

public class CriarPedidoValidator : AbstractValidator<CriarPedidoRequest>
{
    public CriarPedidoValidator()
    {
        RuleFor(x => x.ClienteId).NotEmpty();
        RuleFor(x => x.Itens).NotEmpty().WithMessage("O pedido deve ter ao menos um item.");
        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.ProdutoId).NotEmpty();
            item.RuleFor(i => i.Quantidade).GreaterThan(0).WithMessage("Quantidade deve ser maior que zero.");
        });
    }
}
