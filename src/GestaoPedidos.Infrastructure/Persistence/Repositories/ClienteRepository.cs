using GestaoPedidos.Domain.Entities;
using GestaoPedidos.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestaoPedidos.Infrastructure.Persistence.Repositories;

public class ClienteRepository(AppDbContext context) : IClienteRepository
{
    public async Task<Cliente?> ObterPorIdAsync(Guid id) =>
        await context.Clientes.FindAsync(id);

    public async Task<(IEnumerable<Cliente> itens, int total)> ListarAsync(int pagina, int tamanhoPagina)
    {
        var query = context.Clientes.AsNoTracking().OrderBy(c => c.Nome);
        var total = await query.CountAsync();
        var itens = await query.Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina).ToListAsync();
        return (itens, total);
    }

    public async Task<bool> ExisteEmailAtivoAsync(string email, Guid? ignorarId = null) =>
        await context.Clientes.AnyAsync(c => c.Email == email && c.Ativo && c.Id != ignorarId);

    public async Task<bool> ExisteDocumentoAtivoAsync(string documento, Guid? ignorarId = null) =>
        await context.Clientes.AnyAsync(c => c.Documento == documento && c.Ativo && c.Id != ignorarId);

    public async Task AdicionarAsync(Cliente cliente) =>
        await context.Clientes.AddAsync(cliente);

    public async Task SalvarAlteracoesAsync() =>
        await context.SaveChangesAsync();
}
