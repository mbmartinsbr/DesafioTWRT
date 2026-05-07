using GestaoPedidos.Domain.Entities;

namespace GestaoPedidos.Domain.Interfaces;

public interface IProdutoRepository
{
    Task<Produto?> ObterPorIdAsync(Guid id);
    Task<Produto?> ObterPorIdComLockAsync(Guid id);
    Task<(IEnumerable<Produto> itens, int total)> ListarAsync(int pagina, int tamanhoPagina);
    Task AdicionarAsync(Produto produto);
    void Atualizar(Produto produto);
    Task SalvarAlteracoesAsync();
}
