namespace GestaoPedidos.Application.DTOs.Pedidos;

public record ItemPedidoResponse(
    Guid Id,
    Guid ProdutoId,
    string NomeProduto,
    int Quantidade,
    decimal PrecoUnitario,
    decimal ValorTotal);
