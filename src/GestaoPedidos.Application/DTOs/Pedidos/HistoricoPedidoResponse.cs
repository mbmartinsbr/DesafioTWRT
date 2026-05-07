using GestaoPedidos.Domain.Enums;

namespace GestaoPedidos.Application.DTOs.Pedidos;

public record HistoricoPedidoResponse(
    Guid Id,
    StatusPedido? StatusAnterior,
    StatusPedido NovoStatus,
    DateTime AlteradoEm,
    string? Motivo);
