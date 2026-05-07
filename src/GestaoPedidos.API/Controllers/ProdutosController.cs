using GestaoPedidos.Application.DTOs.Common;
using GestaoPedidos.Application.DTOs.Produtos;
using GestaoPedidos.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestaoPedidos.API.Controllers;

/// <summary>Gerenciamento de produtos e estoque.</summary>
[ApiController]
[Route("api/produtos")]
[Produces("application/json")]
[Tags("Produtos")]
public class ProdutosController(ProdutoService service) : ControllerBase
{
    /// <summary>Cadastra um novo produto.</summary>
    /// <remarks>O preço deve ser maior que zero. O estoque inicial deve ser maior ou igual a zero.</remarks>
    /// <response code="201">Produto criado com sucesso.</response>
    /// <response code="422">Dados de entrada inválidos (preço zero ou negativo, nome vazio, etc.).</response>
    [HttpPost]
    [ProducesResponseType(typeof(ProdutoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar([FromBody] CriarProdutoRequest request)
    {
        var response = await service.CriarAsync(request);
        return CreatedAtAction(nameof(ObterPorId), new { id = response.Id }, response);
    }

    /// <summary>Lista produtos com paginação.</summary>
    /// <param name="pagina">Número da página (começa em 1).</param>
    /// <param name="tamanhoPagina">Itens por página (padrão: 20, máx: 100).</param>
    /// <response code="200">Lista paginada de produtos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProdutoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 20)
        => Ok(await service.ListarAsync(pagina, tamanhoPagina));

    /// <summary>Busca um produto pelo ID.</summary>
    /// <param name="id">ID do produto.</param>
    /// <response code="200">Produto encontrado.</response>
    /// <response code="404">Produto não encontrado.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProdutoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id)
        => Ok(await service.ObterPorIdAsync(id));

    /// <summary>Atualiza nome e descrição de um produto.</summary>
    /// <param name="id">ID do produto.</param>
    /// <param name="request">Novos dados do produto.</param>
    /// <response code="200">Produto atualizado.</response>
    /// <response code="404">Produto não encontrado.</response>
    /// <response code="422">Dados de entrada inválidos.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProdutoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarProdutoRequest request)
        => Ok(await service.AtualizarAsync(id, request));

    /// <summary>Atualiza o preço de um produto.</summary>
    /// <remarks>
    /// A alteração de preço não afeta pedidos já criados — o preço é capturado como snapshot no momento da criação do pedido.
    /// </remarks>
    /// <param name="id">ID do produto.</param>
    /// <param name="request">Novo preço (deve ser maior que zero).</param>
    /// <response code="200">Preço atualizado com sucesso.</response>
    /// <response code="404">Produto não encontrado.</response>
    /// <response code="422">Preço inválido (zero ou negativo).</response>
    [HttpPatch("{id:guid}/preco")]
    [ProducesResponseType(typeof(ProdutoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AtualizarPreco(Guid id, [FromBody] AtualizarPrecoRequest request)
        => Ok(await service.AtualizarPrecoAsync(id, request.Preco));

    /// <summary>Ativa ou desativa um produto (soft delete).</summary>
    /// <param name="id">ID do produto.</param>
    /// <param name="request">Novo estado desejado.</param>
    /// <response code="200">Status alterado com sucesso.</response>
    /// <response code="404">Produto não encontrado.</response>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ProdutoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AlterarStatus(Guid id, [FromBody] AlterarStatusProdutoRequest request)
        => Ok(await service.AlterarStatusAsync(id, request.Ativo));

    /// <summary>Ajusta manualmente o estoque de um produto.</summary>
    /// <remarks>
    /// Use um valor **positivo** para repor estoque e **negativo** para dar baixa manual.
    /// O estoque resultante não pode ficar negativo.
    /// </remarks>
    /// <param name="id">ID do produto.</param>
    /// <param name="request">Quantidade a ajustar (positivo = entrada, negativo = saída).</param>
    /// <response code="200">Estoque atualizado.</response>
    /// <response code="400">Ajuste resultaria em estoque negativo.</response>
    /// <response code="404">Produto não encontrado.</response>
    [HttpPatch("{id:guid}/estoque")]
    [ProducesResponseType(typeof(ProdutoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AtualizarEstoque(Guid id, [FromBody] AtualizarEstoqueRequest request)
        => Ok(await service.AtualizarEstoqueAsync(id, request.Quantidade));
}
