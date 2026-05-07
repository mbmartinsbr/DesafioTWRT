namespace GestaoPedidos.Domain.Interfaces;

public interface IUnitOfWork
{
    Task<IAsyncDisposable> BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
