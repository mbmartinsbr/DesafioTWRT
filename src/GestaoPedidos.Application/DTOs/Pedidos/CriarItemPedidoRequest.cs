namespace GestaoPedidos.Application.DTOs.Pedidos;

public record CriarItemPedidoRequest(Guid ProdutoId, int Quantidade);
