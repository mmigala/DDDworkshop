namespace DDDworkshop.Dam.Rights.Tests.Domain.ValueObjects;

using DDDworkshop.Dam.Rights.Domain.ValueObjects;
using DDDworkshop.Dam.Rights.Domain.Exceptions;

/// <summary>
/// Pure domain tests for the TimeWindow value object.
/// 
/// DDD BENEFIT: These tests need zero infrastructure — no database, no mocking,
/// no service setup. Just instantiate a value object and assert.
/// Try doing this in the Non-DDD project where time validation is scattered
/// across service methods with raw DateTimeOffset parameters!
/// </summary>
public class TimeWindowTests
{
    [Fact]
    public void Constructor_ValidRange_CreatesTimeWindow()
    {
        var start = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var window = new TimeWindow(start, end);

        Assert.Equal(start, window.Start);
        Assert.Equal(end, window.End);
    }

    [Fact]
    public void Constructor_StartEqualsEnd_ThrowsInvalidTimeWindowException()
    {
        var date = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

        Assert.Throws<InvalidTimeWindowException>(() => new TimeWindow(date, date));
    }

    [Fact]
    public void Constructor_StartAfterEnd_ThrowsInvalidTimeWindowException()
    {
        var start = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

        Assert.Throws<InvalidTimeWindowException>(() => new TimeWindow(start, end));
    }

    [Fact]
    public void Duration_ReturnsCorrectSpan()
    {
        var window = MakeWindow(2026, 3, 1, 2026, 6, 1);

        Assert.Equal(TimeSpan.FromDays(92), window.Duration);
    }

    [Fact]
    public void OverlapsWith_OverlappingWindows_ReturnsTrue()
    {
        var a = MakeWindow(2026, 1, 1, 2026, 6, 1);
        var b = MakeWindow(2026, 3, 1, 2026, 9, 1);

        Assert.True(a.OverlapsWith(b));
        Assert.True(b.OverlapsWith(a));
    }

    [Fact]
    public void OverlapsWith_AdjacentWindows_ReturnsFalse()
    {
        var a = MakeWindow(2026, 1, 1, 2026, 3, 1);
        var b = MakeWindow(2026, 3, 1, 2026, 6, 1);

        Assert.False(a.OverlapsWith(b));
        Assert.False(b.OverlapsWith(a));
    }

    [Fact]
    public void OverlapsWith_DisjointWindows_ReturnsFalse()
    {
        var a = MakeWindow(2026, 1, 1, 2026, 3, 1);
        var b = MakeWindow(2026, 6, 1, 2026, 9, 1);

        Assert.False(a.OverlapsWith(b));
    }

    [Fact]
    public void Contains_PointInsideWindow_ReturnsTrue()
    {
        var window = MakeWindow(2026, 1, 1, 2026, 6, 1);
        var point = new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero);

        Assert.True(window.Contains(point));
    }

    [Fact]
    public void Contains_PointAtStart_ReturnsTrue()
    {
        var window = MakeWindow(2026, 1, 1, 2026, 6, 1);
        var point = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        Assert.True(window.Contains(point));
    }

    [Fact]
    public void Contains_PointAtEnd_ReturnsFalse()
    {
        var window = MakeWindow(2026, 1, 1, 2026, 6, 1);
        var point = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

        Assert.False(window.Contains(point));
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = MakeWindow(2026, 1, 1, 2026, 6, 1);
        var b = MakeWindow(2026, 1, 1, 2026, 6, 1);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = MakeWindow(2026, 1, 1, 2026, 6, 1);
        var b = MakeWindow(2026, 1, 1, 2026, 7, 1);

        Assert.NotEqual(a, b);
    }

    private static TimeWindow MakeWindow(int startYear, int startMonth, int startDay, int endYear, int endMonth, int endDay) =>
        new(new DateTimeOffset(startYear, startMonth, startDay, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(endYear, endMonth, endDay, 0, 0, 0, TimeSpan.Zero));
}
