using GestaoPedidos.Domain.Enums;

namespace GestaoPedidos.Application.DTOs.Pedidos;

public record AlterarStatusPedidoRequest(StatusPedido NovoStatus, string? Motivo);
