using GestaoPedidos.Application.DTOs.Common;
using GestaoPedidos.Application.DTOs.Pedidos;
using GestaoPedidos.Application.Exceptions;
using GestaoPedidos.Domain.Entities;
using GestaoPedidos.Domain.Interfaces;

namespace GestaoPedidos.Application.Services;

public class PedidoService(
    IPedidoRepository pedidoRepository,
    IClienteRepository clienteRepository,
    IProdutoRepository produtoRepository,
    IUnitOfWork uow)
{
    public async Task<PedidoResponse> CriarAsync(CriarPedidoRequest request)
    {
        var cliente = await clienteRepository.ObterPorIdAsync(request.ClienteId)
            ?? throw new NotFoundException($"Cliente '{request.ClienteId}' não encontrado.");

        if (!cliente.Ativo)
            throw new BusinessException("Cliente inativo não pode realizar pedidos.");

        await using var _ = await uow.BeginTransactionAsync();
        try
        {
            var pedidoId = Guid.NewGuid();
            var itensPedido = new List<ItemPedido>();

            foreach (var itemReq in request.Itens)
            {
                var produto = await produtoRepository.ObterPorIdComLockAsync(itemReq.ProdutoId)
                    ?? throw new NotFoundException($"Produto '{itemReq.ProdutoId}' não encontrado.");

                if (!produto.Ativo)
                    throw new BusinessException($"Produto '{produto.Nome}' está inativo.");

                if (!produto.DebitarEstoque(itemReq.Quantidade))
                    throw new BusinessException($"Estoque insuficiente para o produto '{produto.Nome}'. Disponível: {produto.Estoque}.");

                itensPedido.Add(ItemPedido.Criar(pedidoId, produto.Id, itemReq.Quantidade, produto.Preco));
            }

            var pedido = Pedido.Criar(cliente.Id, itensPedido, pedidoId);
            await pedidoRepository.AdicionarAsync(pedido);
            await uow.CommitAsync();

            return ToResponse(pedido, cliente.Nome);
        }
        catch
        {
            await uow.RollbackAsync();
            throw;
        }
    }

    public async Task<PagedResult<PedidoResponse>> ListarAsync(int pagina, int tamanhoPagina)
    {
        var (itens, total) = await pedidoRepository.ListarAsync(pagina, tamanhoPagina);
        var responses = itens.Select(p => ToResponse(p, p.Cliente?.Nome ?? string.Empty));
        return new PagedResult<PedidoResponse>(responses, pagina, tamanhoPagina, total);
    }

    public async Task<PedidoResponse> ObterPorIdAsync(Guid id)
    {
        var pedido = await pedidoRepository.ObterPorIdAsync(id)
            ?? throw new NotFoundException($"Pedido '{id}' não encontrado.");
        return ToResponse(pedido, pedido.Cliente.Nome);
    }

    public async Task<PedidoResponse> AlterarStatusAsync(Guid id, AlterarStatusPedidoRequest request)
    {
        var pedido = await pedidoRepository.ObterParaAtualizacaoAsync(id)
            ?? throw new NotFoundException($"Pedido '{id}' não encontrado.");

        var statusAnterior = pedido.Status;
        var (sucesso, erro) = pedido.AlterarStatus(request.NovoStatus, request.Motivo);
        if (!sucesso)
            throw new BusinessException(erro!);

        if (request.NovoStatus == Domain.Enums.StatusPedido.Cancelado && pedido.PodeRetornarEstoque())
        {
            foreach (var item in pedido.Itens)
            {
                var produto = await produtoRepository.ObterPorIdAsync(item.ProdutoId);
                if (produto is null)
                    throw new NotFoundException($"Produto '{item.ProdutoId}' não encontrado ao devolver estoque.");
                produto.DevolverEstoque(item.Quantidade);
                produtoRepository.Atualizar(produto);
            }
            await produtoRepository.SalvarAlteracoesAsync();
        }

        var novoHistorico = Domain.Entities.HistoricoPedido.Criar(id, statusAnterior, request.NovoStatus, request.Motivo);
        await pedidoRepository.AtualizarStatusAsync(id, request.NovoStatus, novoHistorico);

        var pedidoAtualizado = await pedidoRepository.ObterPorIdAsync(id);
        return ToResponse(pedidoAtualizado!, pedidoAtualizado!.Cliente.Nome);
    }

    private static PedidoResponse ToResponse(Pedido p, string nomeCliente) => new(
        p.Id,
        p.ClienteId,
        nomeCliente,
        p.Status,
        p.ValorTotal,
        p.CriadoEm,
        p.Itens.Select(i => new ItemPedidoResponse(
            i.Id, i.ProdutoId, i.Produto?.Nome ?? string.Empty,
            i.Quantidade, i.PrecoUnitario, i.ValorTotal)),
        p.Historico.Select(h => new HistoricoPedidoResponse(
            h.Id, h.StatusAnterior, h.NovoStatus, h.AlteradoEm, h.Motivo)));
}
