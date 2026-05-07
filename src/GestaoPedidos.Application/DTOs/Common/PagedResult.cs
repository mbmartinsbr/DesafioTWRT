namespace GestaoPedidos.Application.DTOs.Common;

public record PagedResult<T>(
    IEnumerable<T> Dados,
    int Pagina,
    int TamanhoPagina,
    int Total)
{
    public int TotalPaginas => (int)Math.Ceiling((double)Total / TamanhoPagina);
}
