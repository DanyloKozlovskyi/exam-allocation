namespace Application.Common.Interfaces;

/// <summary>Runs work in one database transaction (unit of work); commits on success, rolls back on exception.</summary>
public interface ITransactionRunner
{
    Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken);
}