namespace DDDworkshop.Dam.Rights.Application.Commands;

/// <summary>
/// Command to revoke an existing license grant.
/// </summary>
public sealed record RevokeLicenseCommand(
    Guid GrantId,
    string Reason,
    string RevokedBy);
