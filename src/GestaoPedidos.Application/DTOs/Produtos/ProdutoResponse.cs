namespace GestaoPedidos.Application.DTOs.Produtos;

public record ProdutoResponse(
    Guid Id,
    string Nome,
    string? Descricao,
    decimal Preco,
    int Estoque,
    bool Ativo,
    DateTime CriadoEm,
    DateTime AtualizadoEm);
