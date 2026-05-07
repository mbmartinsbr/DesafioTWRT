using GestaoPedidos.Domain.Enums;

namespace GestaoPedidos.Application.DTOs.Pedidos;

public record PedidoResponse(
    Guid Id,
    Guid ClienteId,
    string NomeCliente,
    StatusPedido Status,
    decimal ValorTotal,
    DateTime CriadoEm,
    IEnumerable<ItemPedidoResponse> Itens,
    IEnumerable<HistoricoPedidoResponse> Historico);
