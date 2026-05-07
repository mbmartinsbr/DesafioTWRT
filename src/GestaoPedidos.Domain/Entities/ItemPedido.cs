namespace GestaoPedidos.Domain.Entities;

public class ItemPedido
{
    public Guid Id { get; private set; }
    public Guid PedidoId { get; private set; }
    public Guid ProdutoId { get; private set; }
    public int Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public decimal ValorTotal { get; private set; }

    public Produto Produto { get; private set; } = null!;

    private ItemPedido() { }

    public static ItemPedido Criar(Guid pedidoId, Guid produtoId, int quantidade, decimal precoUnitario)
    {
        return new ItemPedido
        {
            Id = Guid.NewGuid(),
            PedidoId = pedidoId,
            ProdutoId = produtoId,
            Quantidade = quantidade,
            PrecoUnitario = precoUnitario,
            ValorTotal = Math.Round(quantidade * precoUnitario, 2, MidpointRounding.AwayFromZero)
        };
    }
}
