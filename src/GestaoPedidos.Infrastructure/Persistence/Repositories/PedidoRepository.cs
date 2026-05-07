using GestaoPedidos.Domain.Entities;
using GestaoPedidos.Domain.Enums;
using GestaoPedidos.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestaoPedidos.Infrastructure.Persistence.Repositories;

public class PedidoRepository(AppDbContext context) : IPedidoRepository
{
    public async Task<Pedido?> ObterPorIdAsync(Guid id)
    {
        var pedido = await context.Pedidos
            .Include(p => p.Itens)
            .Include(p => p.Historico)
            .Include(p => p.Cliente)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pedido != null)
        {
            foreach (var item in pedido.Itens)
                await context.Entry(item).Reference(i => i.Produto).LoadAsync();
        }

        return pedido;
    }

    public async Task<(IEnumerable<Pedido> itens, int total)> ListarAsync(int pagina, int tamanhoPagina)
    {
        var query = context.Pedidos.AsNoTracking()
            .Include(p => p.Itens)
            .OrderByDescending(p => p.CriadoEm);
        var total = await query.CountAsync();
        var itens = await query.Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina).ToListAsync();
        return (itens, total);
    }

    public async Task<Pedido?> ObterParaAtualizacaoAsync(Guid id) =>
        await context.Pedidos
            .AsNoTracking()
            .Include(p => p.Itens)
            .Include(p => p.Cliente)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task AtualizarStatusAsync(Guid pedidoId, StatusPedido novoStatus, HistoricoPedido novoHistorico)
    {
        await context.Pedidos
            .Where(p => p.Id == pedidoId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, novoStatus));

        await context.HistoricosPedido.AddAsync(novoHistorico);
        await context.SaveChangesAsync();
    }

    public async Task AdicionarAsync(Pedido pedido) =>
        await context.Pedidos.AddAsync(pedido);

    public async Task SalvarAlteracoesAsync() =>
        await context.SaveChangesAsync();
}
