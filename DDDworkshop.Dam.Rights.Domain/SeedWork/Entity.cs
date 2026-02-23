namespace DDDworkshop.Dam.Rights.Domain.SeedWork;

/// <summary>
/// Base class for entities – objects with a unique identity that persists over time.
/// Two entities are equal if they have the same Id, regardless of their other properties.
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    public TId Id { get; protected init; }

    protected Entity(TId id)
    {
        Id = id;
    }

    // EF / serialization
    protected Entity() => Id = default!;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
            return false;

        return Equals(other);
    }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
        => !Equals(left, right);
}
