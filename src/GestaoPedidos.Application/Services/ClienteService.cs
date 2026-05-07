using GestaoPedidos.Application.DTOs.Clientes;
using GestaoPedidos.Application.DTOs.Common;
using GestaoPedidos.Application.Exceptions;
using GestaoPedidos.Domain.Entities;
using GestaoPedidos.Domain.Interfaces;

namespace GestaoPedidos.Application.Services;

public class ClienteService(IClienteRepository repository)
{
    public async Task<ClienteResponse> CriarAsync(CriarClienteRequest request)
    {
        if (await repository.ExisteEmailAtivoAsync(request.Email))
            throw new ConflictException("Já existe um cliente ativo com este e-mail.");

        if (await repository.ExisteDocumentoAtivoAsync(request.Documento))
            throw new ConflictException("Já existe um cliente ativo com este documento.");

        var cliente = Cliente.Criar(request.Nome, request.Email.ToLowerInvariant(), request.Documento);
        await repository.AdicionarAsync(cliente);
        await repository.SalvarAlteracoesAsync();

        return ToResponse(cliente);
    }

    public async Task<PagedResult<ClienteResponse>> ListarAsync(int pagina, int tamanhoPagina)
    {
        var (itens, total) = await repository.ListarAsync(pagina, tamanhoPagina);
        return new PagedResult<ClienteResponse>(itens.Select(ToResponse), pagina, tamanhoPagina, total);
    }

    public async Task<ClienteResponse> ObterPorIdAsync(Guid id)
    {
        var cliente = await repository.ObterPorIdAsync(id)
            ?? throw new NotFoundException($"Cliente '{id}' não encontrado.");
        return ToResponse(cliente);
    }

    public async Task<ClienteResponse> AlterarStatusAsync(Guid id, bool ativo)
    {
        var cliente = await repository.ObterPorIdAsync(id)
            ?? throw new NotFoundException($"Cliente '{id}' não encontrado.");

        if (ativo) cliente.Ativar(); else cliente.Desativar();
        await repository.SalvarAlteracoesAsync();

        return ToResponse(cliente);
    }

    private static ClienteResponse ToResponse(Cliente c) =>
        new(c.Id, c.Nome, c.Email, c.Documento, c.Ativo, c.CriadoEm, c.AtualizadoEm);
}
