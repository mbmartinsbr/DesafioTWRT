namespace GestaoPedidos.Application.DTOs.Pedidos;

public record CriarPedidoRequest(Guid ClienteId, List<CriarItemPedidoRequest> Itens);
