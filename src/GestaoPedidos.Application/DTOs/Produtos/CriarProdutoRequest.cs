namespace GestaoPedidos.Application.DTOs.Produtos;

public record CriarProdutoRequest(string Nome, string? Descricao, decimal Preco, int Estoque);
