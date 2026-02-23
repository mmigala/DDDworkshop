namespace DDDworkshop.Dam.NoDdd.Api.Data;

using DDDworkshop.Dam.NoDdd.Api.Entities;
using System.Collections.Concurrent;

// ⚠️ ANTI-PATTERN: Global mutable data store accessible from anywhere.
// Any service can read/write any entity at any time with no aggregate boundary.
// Compare to the DDD approach where repositories are behind interfaces and each
// aggregate root controls access to its child entities.

/// <summary>
/// Simple in-memory data store using ConcurrentDictionaries.
/// Acts as a stand-in for a database context (EF Core DbContext).
/// </summary>
public class InMemoryDataStore
{
    // ⚠️ All entities in flat dictionaries — no aggregate boundaries.
    // A service can grab a RestrictionEntity and mutate it without going through any aggregate root.
    public ConcurrentDictionary<Guid, AssetEntity> Assets { get; } = new();
    public ConcurrentDictionary<Guid, RestrictionEntity> Restrictions { get; } = new();
    public ConcurrentDictionary<Guid, ExclusiveWindowEntity> ExclusiveWindows { get; } = new();
    public ConcurrentDictionary<Guid, LicenseGrantEntity> LicenseGrants { get; } = new();
}
