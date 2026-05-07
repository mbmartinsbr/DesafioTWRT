namespace GestaoPedidos.Application.DTOs.Clientes;

public record ClienteResponse(
    Guid Id,
    string Nome,
    string Email,
    string Documento,
    bool Ativo,
    DateTime CriadoEm,
    DateTime AtualizadoEm);
