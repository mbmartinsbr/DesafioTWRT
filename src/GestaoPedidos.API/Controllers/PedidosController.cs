using GestaoPedidos.Application.DTOs.Common;
using GestaoPedidos.Application.DTOs.Pedidos;
using GestaoPedidos.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestaoPedidos.API.Controllers;

/// <summary>Gerenciamento de pedidos.</summary>
[ApiController]
[Route("api/pedidos")]
[Produces("application/json")]
[Tags("Pedidos")]
public class PedidosController(PedidoService service) : ControllerBase
{
    /// <summary>Cria um novo pedido.</summary>
    /// <remarks>
    /// O fluxo transacional garante que:
    /// - O cliente existe e está ativo
    /// - Todos os produtos existem, estão ativos e possuem estoque suficiente
    /// - O estoque é debitado atomicamente para todos os itens
    /// - O preço unitário de cada item é capturado no momento da criação (snapshot)
    ///
    /// Transições de status permitidas:
    /// - `Criado` → `Pago` | `Cancelado`
    /// - `Pago` → `Enviado`
    /// - `Enviado` → _(nenhuma)_
    /// - `Cancelado` → _(nenhuma)_
    /// </remarks>
    /// <response code="201">Pedido criado com sucesso.</response>
    /// <response code="400">Cliente inativo, produto inativo ou estoque insuficiente.</response>
    /// <response code="404">Cliente ou produto não encontrado.</response>
    /// <response code="422">Dados de entrada inválidos (lista de itens vazia, quantidade zero, etc.).</response>
    [HttpPost]
    [ProducesResponseType(typeof(PedidoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar([FromBody] CriarPedidoRequest request)
    {
        var response = await service.CriarAsync(request);
        return CreatedAtAction(nameof(ObterPorId), new { id = response.Id }, response);
    }

    /// <summary>Lista pedidos com paginação.</summary>
    /// <param name="pagina">Número da página (começa em 1).</param>
    /// <param name="tamanhoPagina">Itens por página (padrão: 20, máx: 100).</param>
    /// <response code="200">Lista paginada de pedidos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PedidoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 20)
        => Ok(await service.ListarAsync(pagina, tamanhoPagina));

    /// <summary>Busca um pedido pelo ID (inclui itens e histórico de status).</summary>
    /// <param name="id">ID do pedido.</param>
    /// <response code="200">Pedido encontrado com itens e histórico.</response>
    /// <response code="404">Pedido não encontrado.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PedidoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id)
        => Ok(await service.ObterPorIdAsync(id));

    /// <summary>Altera o status de um pedido.</summary>
    /// <remarks>
    /// Transições permitidas:
    /// - `Criado` → `Pago` | `Cancelado`
    /// - `Pago` → `Enviado`
    ///
    /// Enviar o mesmo status atual retorna `400` sem registrar histórico.
    ///
    /// Ao cancelar um pedido com status `Criado` ou `Pago`, o estoque dos produtos é devolvido automaticamente.
    /// Pedidos com status `Enviado` não aceitam mais alterações.
    /// </remarks>
    /// <param name="id">ID do pedido.</param>
    /// <param name="request">Novo status e motivo opcional.</param>
    /// <response code="200">Status alterado com sucesso.</response>
    /// <response code="400">Transição inválida ou status igual ao atual.</response>
    /// <response code="404">Pedido não encontrado.</response>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(PedidoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AlterarStatus(Guid id, [FromBody] AlterarStatusPedidoRequest request)
        => Ok(await service.AlterarStatusAsync(id, request));
}
