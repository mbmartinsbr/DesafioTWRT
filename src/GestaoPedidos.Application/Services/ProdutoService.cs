using GestaoPedidos.Application.DTOs.Common;
using GestaoPedidos.Application.DTOs.Produtos;
using GestaoPedidos.Application.Exceptions;
using GestaoPedidos.Domain.Entities;
using GestaoPedidos.Domain.Interfaces;

namespace GestaoPedidos.Application.Services;

public class ProdutoService(IProdutoRepository repository)
{
    public async Task<ProdutoResponse> CriarAsync(CriarProdutoRequest request)
    {
        var produto = Produto.Criar(request.Nome, request.Descricao, request.Preco, request.Estoque);
        await repository.AdicionarAsync(produto);
        await repository.SalvarAlteracoesAsync();
        return ToResponse(produto);
    }

    public async Task<PagedResult<ProdutoResponse>> ListarAsync(int pagina, int tamanhoPagina)
    {
        var (itens, total) = await repository.ListarAsync(pagina, tamanhoPagina);
        return new PagedResult<ProdutoResponse>(itens.Select(ToResponse), pagina, tamanhoPagina, total);
    }

    public async Task<ProdutoResponse> ObterPorIdAsync(Guid id)
    {
        var produto = await repository.ObterPorIdAsync(id)
            ?? throw new NotFoundException($"Produto '{id}' não encontrado.");
        return ToResponse(produto);
    }

    public async Task<ProdutoResponse> AtualizarAsync(Guid id, AtualizarProdutoRequest request)
    {
        var produto = await repository.ObterPorIdAsync(id)
            ?? throw new NotFoundException($"Produto '{id}' não encontrado.");

        produto.Atualizar(request.Nome, request.Descricao);
        await repository.SalvarAlteracoesAsync();
        return ToResponse(produto);
    }

    public async Task<ProdutoResponse> AlterarStatusAsync(Guid id, bool ativo)
    {
        var produto = await repository.ObterPorIdAsync(id)
            ?? throw new NotFoundException($"Produto '{id}' não encontrado.");

        if (ativo) produto.Ativar(); else produto.Inativar();
        await repository.SalvarAlteracoesAsync();
        return ToResponse(produto);
    }

    public async Task<ProdutoResponse> AtualizarPrecoAsync(Guid id, decimal preco)
    {
        var produto = await repository.ObterPorIdAsync(id)
            ?? throw new NotFoundException($"Produto '{id}' não encontrado.");

        produto.AtualizarPreco(preco);
        await repository.SalvarAlteracoesAsync();
        return ToResponse(produto);
    }

    public async Task<ProdutoResponse> AtualizarEstoqueAsync(Guid id, int quantidade)
    {
        var produto = await repository.ObterPorIdAsync(id)
            ?? throw new NotFoundException($"Produto '{id}' não encontrado.");

        if (!produto.Ativo)
            throw new BusinessException("Produto inativo não pode ter estoque ajustado.");

        if (quantidade < 0)
            throw new BusinessException("Estoque não pode ser negativo.");

        produto.AtualizarEstoque(quantidade);
        await repository.SalvarAlteracoesAsync();
        return ToResponse(produto);
    }

    private static ProdutoResponse ToResponse(Produto p) =>
        new(p.Id, p.Nome, p.Descricao, p.Preco, p.Estoque, p.Ativo, p.CriadoEm, p.AtualizadoEm);
}
