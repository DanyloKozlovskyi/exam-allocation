namespace Domain;

/// <summary>Optimistic concurrency token (mapped to PostgreSQL xmin).</summary>
public interface IVersioned
{
    uint Version { get; }
}