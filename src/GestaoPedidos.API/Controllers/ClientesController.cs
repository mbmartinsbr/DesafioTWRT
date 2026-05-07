using GestaoPedidos.Application.DTOs.Clientes;
using GestaoPedidos.Application.DTOs.Common;
using GestaoPedidos.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestaoPedidos.API.Controllers;

/// <summary>Gerenciamento de clientes.</summary>
[ApiController]
[Route("api/clientes")]
[Produces("application/json")]
[Tags("Clientes")]
public class ClientesController(ClienteService service) : ControllerBase
{
    /// <summary>Cadastra um novo cliente.</summary>
    /// <remarks>
    /// O documento pode ser CPF (11 dígitos) ou CNPJ (14 dígitos) — somente números.
    /// Email e documento devem ser únicos entre clientes **ativos**.
    /// </remarks>
    /// <response code="201">Cliente criado com sucesso.</response>
    /// <response code="409">Email ou documento já cadastrado em cliente ativo.</response>
    /// <response code="422">Dados de entrada inválidos (CPF/CNPJ inválido, email mal formado, etc.).</response>
    [HttpPost]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar([FromBody] CriarClienteRequest request)
    {
        var response = await service.CriarAsync(request);
        return CreatedAtAction(nameof(ObterPorId), new { id = response.Id }, response);
    }

    /// <summary>Lista clientes com paginação.</summary>
    /// <param name="pagina">Número da página (começa em 1).</param>
    /// <param name="tamanhoPagina">Itens por página (padrão: 20, máx: 100).</param>
    /// <response code="200">Lista paginada de clientes.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ClienteResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 20)
        => Ok(await service.ListarAsync(pagina, tamanhoPagina));

    /// <summary>Busca um cliente pelo ID.</summary>
    /// <param name="id">ID do cliente.</param>
    /// <response code="200">Cliente encontrado.</response>
    /// <response code="404">Cliente não encontrado.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id)
        => Ok(await service.ObterPorIdAsync(id));

    /// <summary>Ativa ou desativa um cliente (soft delete).</summary>
    /// <param name="id">ID do cliente.</param>
    /// <param name="request">Novo estado desejado.</param>
    /// <response code="200">Status alterado com sucesso.</response>
    /// <response code="404">Cliente não encontrado.</response>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AlterarStatus(Guid id, [FromBody] AlterarStatusClienteRequest request)
        => Ok(await service.AlterarStatusAsync(id, request.Ativo));
}
