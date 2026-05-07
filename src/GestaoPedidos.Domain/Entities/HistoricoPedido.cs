using GestaoPedidos.Domain.Enums;

namespace GestaoPedidos.Domain.Entities;

public class HistoricoPedido
{
    public Guid Id { get; private set; }
    public Guid PedidoId { get; private set; }
    public StatusPedido? StatusAnterior { get; private set; }
    public StatusPedido NovoStatus { get; private set; }
    public DateTime AlteradoEm { get; private set; }
    public string? Motivo { get; private set; }

    private HistoricoPedido() { }

    public static HistoricoPedido Criar(Guid pedidoId, StatusPedido? statusAnterior, StatusPedido novoStatus, string? motivo = null)
    {
        return new HistoricoPedido
        {
            Id = Guid.NewGuid(),
            PedidoId = pedidoId,
            StatusAnterior = statusAnterior,
            NovoStatus = novoStatus,
            AlteradoEm = DateTime.UtcNow,
            Motivo = motivo
        };
    }
}
