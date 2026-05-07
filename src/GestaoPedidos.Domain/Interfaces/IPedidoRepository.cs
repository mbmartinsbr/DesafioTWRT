using GestaoPedidos.Domain.Entities;
using GestaoPedidos.Domain.Enums;

namespace GestaoPedidos.Domain.Interfaces;

public interface IPedidoRepository
{
    Task<Pedido?> ObterPorIdAsync(Guid id);
    Task<Pedido?> ObterParaAtualizacaoAsync(Guid id);
    Task AtualizarStatusAsync(Guid pedidoId, StatusPedido novoStatus, HistoricoPedido novoHistorico);
    Task<(IEnumerable<Pedido> itens, int total)> ListarAsync(int pagina, int tamanhoPagina);
    Task AdicionarAsync(Pedido pedido);
    Task SalvarAlteracoesAsync();
}
