using GestaoPedidos.Domain.Entities;
using GestaoPedidos.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestaoPedidos.Infrastructure.Persistence.Repositories;

public class ProdutoRepository(AppDbContext context) : IProdutoRepository
{
    public async Task<Produto?> ObterPorIdAsync(Guid id) =>
        await context.Produtos.FindAsync(id);

    public async Task<Produto?> ObterPorIdComLockAsync(Guid id)
    {
        if (!context.Database.IsRelational())
            return await context.Produtos.FirstOrDefaultAsync(p => p.Id == id);

        return await context.Produtos
            .FromSqlRaw("SELECT * FROM \"Produtos\" WHERE \"Id\" = {0} FOR UPDATE", id)
            .FirstOrDefaultAsync();
    }

    public async Task<(IEnumerable<Produto> itens, int total)> ListarAsync(int pagina, int tamanhoPagina)
    {
        var query = context.Produtos.AsNoTracking().OrderBy(p => p.Nome);
        var total = await query.CountAsync();
        var itens = await query.Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina).ToListAsync();
        return (itens, total);
    }

    public async Task AdicionarAsync(Produto produto) =>
        await context.Produtos.AddAsync(produto);

    public void Atualizar(Produto produto) =>
        context.Entry(produto).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

    public async Task SalvarAlteracoesAsync() =>
        await context.SaveChangesAsync();
}
