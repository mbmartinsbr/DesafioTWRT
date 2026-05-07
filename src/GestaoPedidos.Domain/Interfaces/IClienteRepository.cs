using GestaoPedidos.Domain.Entities;

namespace GestaoPedidos.Domain.Interfaces;

public interface IClienteRepository
{
    Task<Cliente?> ObterPorIdAsync(Guid id);
    Task<(IEnumerable<Cliente> itens, int total)> ListarAsync(int pagina, int tamanhoPagina);
    Task<bool> ExisteEmailAtivoAsync(string email, Guid? ignorarId = null);
    Task<bool> ExisteDocumentoAtivoAsync(string documento, Guid? ignorarId = null);
    Task AdicionarAsync(Cliente cliente);
    Task SalvarAlteracoesAsync();
}
