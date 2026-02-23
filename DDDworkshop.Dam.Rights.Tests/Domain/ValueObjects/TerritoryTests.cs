namespace DDDworkshop.Dam.Rights.Tests.Domain.ValueObjects;

using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// Pure domain tests for the Territory value object.
/// 
/// DDD BENEFIT: Territory overlap detection is a single, testable method on a value object.
/// In the Non-DDD project, territory is a raw comma-separated string and overlap
/// detection is scattered across service methods with inline Split() calls.
/// </summary>
public class TerritoryTests
{
    [Fact]
    public void Constructor_ValidCodes_CreatesTerritory()
    {
        var territory = new Territory(["NO", "SE", "DK"]);

        Assert.Equal(3, territory.CountryCodes.Count);
        Assert.Contains("NO", territory.CountryCodes);
        Assert.Contains("SE", territory.CountryCodes);
        Assert.Contains("DK", territory.CountryCodes);
    }

    [Fact]
    public void Constructor_NormalizesToUpperCase()
    {
        var territory = new Territory(["no", "se"]);

        Assert.Contains("NO", territory.CountryCodes);
        Assert.Contains("SE", territory.CountryCodes);
    }

    [Fact]
    public void Constructor_EmptyCollection_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Territory([]));
    }

    [Fact]
    public void Constructor_InvalidCodeLength_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Territory(["NOR"]));
    }

    [Fact]
    public void OverlapsWith_SharedCodes_ReturnsTrue()
    {
        var scandinavia = new Territory(["NO", "SE", "DK"]);
        var nordic = new Territory(["NO", "FI", "IS"]);

        Assert.True(scandinavia.OverlapsWith(nordic));
        Assert.True(nordic.OverlapsWith(scandinavia));
    }

    [Fact]
    public void OverlapsWith_DisjointCodes_ReturnsFalse()
    {
        var scandinavia = new Territory(["NO", "SE", "DK"]);
        var northAmerica = new Territory(["US", "CA"]);

        Assert.False(scandinavia.OverlapsWith(northAmerica));
    }

    [Fact]
    public void Contains_Subset_ReturnsTrue()
    {
        var scandinavia = new Territory(["NO", "SE", "DK"]);
        var norway = new Territory(["NO"]);

        Assert.True(scandinavia.Contains(norway));
    }

    [Fact]
    public void Contains_NotSubset_ReturnsFalse()
    {
        var norway = new Territory(["NO"]);
        var scandinavia = new Territory(["NO", "SE", "DK"]);

        Assert.False(norway.Contains(scandinavia));
    }

    [Fact]
    public void Equality_SameCodes_AreEqual()
    {
        var a = new Territory(["NO", "SE"]);
        var b = new Territory(["SE", "NO"]); // Different order, same set

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentCodes_AreNotEqual()
    {
        var a = new Territory(["NO", "SE"]);
        var b = new Territory(["NO", "DK"]);

        Assert.NotEqual(a, b);
    }
}
