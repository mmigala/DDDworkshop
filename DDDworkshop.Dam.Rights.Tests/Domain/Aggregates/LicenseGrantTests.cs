namespace DDDworkshop.Dam.Rights.Tests.Domain.Aggregates;

using DDDworkshop.Dam.Rights.Domain.Aggregates.LicenseGrantAggregate;
using DDDworkshop.Dam.Rights.Domain.Events;
using DDDworkshop.Dam.Rights.Domain.Exceptions;
using DDDworkshop.Dam.Rights.Domain.ValueObjects;

/// <summary>
/// Pure domain tests for the LicenseGrant aggregate root.
/// 
/// DDD BENEFIT: Lifecycle transitions (Issue → Revoke, Issue → Expire) and their
/// guard clauses are encoded in the aggregate itself, making them trivially testable.
/// Domain events are also captured and can be asserted.
/// 
/// In the Non-DDD project, these rules live in LicenseService and require setting
/// up InMemoryDataStore + the full service to test a simple state transition.
/// </summary>
public class LicenseGrantTests
{
    private static readonly AssetId TestAssetId = new(Guid.NewGuid());
    private static readonly LicenseeId TestLicenseeId = new(Guid.NewGuid());
    private static readonly DateTimeOffset IssuedAt = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

    // ──────────────────────────────────────────────
    //  Factory Method: Issue
    // ──────────────────────────────────────────────

    [Fact]
    public void Issue_ValidData_CreatesGrantInIssuedState()
    {
        var grant = IssueTestGrant();

        Assert.Equal(GrantStatus.Issued, grant.Status);
        Assert.Equal(TestAssetId, grant.AssetId);
        Assert.Equal(TestLicenseeId, grant.LicenseeId);
        Assert.Equal(IssuedAt, grant.IssuedAt);
    }

    [Fact]
    public void Issue_SetsExpiresAtToTimeWindowEnd()
    {
        var grant = IssueTestGrant();

        Assert.Equal(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero), grant.ExpiresAt);
    }

    [Fact]
    public void Issue_RaisesLicenseGrantedEvent()
    {
        var grant = IssueTestGrant();

        var @event = Assert.Single(grant.DomainEvents);
        var grantedEvent = Assert.IsType<LicenseGrantedEvent>(@event);
        Assert.Equal(TestAssetId, grantedEvent.AssetId);
        Assert.Equal(grant.Id, grantedEvent.GrantId);
    }

    [Fact]
    public void Issue_RecordsStatusHistory()
    {
        var grant = IssueTestGrant();

        Assert.Single(grant.StatusHistory);
        Assert.Equal(GrantStatus.Issued, grant.StatusHistory[0].Status);
    }

    [Fact]
    public void Issue_IssuedAtAfterExpiry_ThrowsInvalidTimeWindowException()
    {
        var lateIssuedAt = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var terms = MakeTerms();

        Assert.Throws<InvalidTimeWindowException>(() =>
            LicenseGrant.Issue(TestAssetId, TestLicenseeId, terms, lateIssuedAt));
    }

    // ──────────────────────────────────────────────
    //  Revoke
    // ──────────────────────────────────────────────

    [Fact]
    public void Revoke_IssuedGrant_TransitionsToRevoked()
    {
        var grant = IssueTestGrant();
        var now = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

        grant.Revoke("Contract breach", "admin@test.com", now);

        Assert.Equal(GrantStatus.Revoked, grant.Status);
        Assert.NotNull(grant.Revocation);
        Assert.Equal("Contract breach", grant.Revocation!.Reason);
        Assert.Equal("admin@test.com", grant.Revocation.RevokedBy);
    }

    [Fact]
    public void Revoke_RaisesLicenseRevokedEvent()
    {
        var grant = IssueTestGrant();
        grant.ClearDomainEvents(); // Clear the LicenseGrantedEvent
        var now = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

        grant.Revoke("Breach", "admin", now);

        var @event = Assert.Single(grant.DomainEvents);
        var revokedEvent = Assert.IsType<LicenseRevokedEvent>(@event);
        Assert.Equal(grant.Id, revokedEvent.GrantId);
    }

    [Fact]
    public void Revoke_AlreadyRevoked_ThrowsDomainException()
    {
        var grant = IssueTestGrant();
        var now = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        grant.Revoke("First", "admin", now);

        Assert.Throws<LicenseGrant.DomainException>(() =>
            grant.Revoke("Second", "admin", now));
    }

    [Fact]
    public void Revoke_ExpiredGrant_ThrowsDomainException()
    {
        var grant = IssueTestGrant();
        var pastExpiry = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);
        grant.MarkExpiredIfPastDue(pastExpiry);

        Assert.Throws<LicenseGrant.DomainException>(() =>
            grant.Revoke("Too late", "admin", pastExpiry));
    }

    // ──────────────────────────────────────────────
    //  Expiry
    // ──────────────────────────────────────────────

    [Fact]
    public void MarkExpiredIfPastDue_PastExpiry_TransitionsToExpired()
    {
        var grant = IssueTestGrant();
        var pastExpiry = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);

        grant.MarkExpiredIfPastDue(pastExpiry);

        Assert.Equal(GrantStatus.Expired, grant.Status);
    }

    [Fact]
    public void MarkExpiredIfPastDue_BeforeExpiry_StaysIssued()
    {
        var grant = IssueTestGrant();
        var beforeExpiry = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);

        grant.MarkExpiredIfPastDue(beforeExpiry);

        Assert.Equal(GrantStatus.Issued, grant.Status);
    }

    [Fact]
    public void MarkExpiredIfPastDue_AlreadyRevoked_RemainsRevoked()
    {
        var grant = IssueTestGrant();
        grant.Revoke("Breach", "admin", new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero));
        var pastExpiry = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);

        grant.MarkExpiredIfPastDue(pastExpiry);

        Assert.Equal(GrantStatus.Revoked, grant.Status);
    }

    // ──────────────────────────────────────────────
    //  IsActive
    // ──────────────────────────────────────────────

    [Fact]
    public void IsActive_IssuedAndBeforeExpiry_ReturnsTrue()
    {
        var grant = IssueTestGrant();
        var now = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);

        Assert.True(grant.IsActive(now));
    }

    [Fact]
    public void IsActive_AfterExpiry_ReturnsFalse()
    {
        var grant = IssueTestGrant();
        var now = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);

        Assert.False(grant.IsActive(now));
    }

    [Fact]
    public void IsActive_Revoked_ReturnsFalse()
    {
        var grant = IssueTestGrant();
        grant.Revoke("Breach", "admin", new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.False(grant.IsActive(new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero)));
    }

    // ──────────────────────────────────────────────
    //  Status History / Audit Trail
    // ──────────────────────────────────────────────

    [Fact]
    public void Revoke_RecordsFullStatusHistory()
    {
        var grant = IssueTestGrant();
        var now = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

        grant.Revoke("Breach", "admin", now);

        Assert.Equal(2, grant.StatusHistory.Count);
        Assert.Equal(GrantStatus.Issued, grant.StatusHistory[0].Status);
        Assert.Equal(GrantStatus.Revoked, grant.StatusHistory[1].Status);
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────

    private static LicenseGrant IssueTestGrant(bool isExclusive = false)
    {
        return LicenseGrant.Issue(TestAssetId, TestLicenseeId, MakeTerms(isExclusive), IssuedAt);
    }

    private static LicenseTerms MakeTerms(bool isExclusive = false)
    {
        var territory = new Territory(["NO"]);
        var timeWindow = new TimeWindow(
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        var scope = new LicenseScope(UsageChannel.Web, territory, timeWindow, UsagePurpose.Editorial);
        return new LicenseTerms(scope, isExclusive);
    }
}
