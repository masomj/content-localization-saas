namespace ContentLocalizationSaaS.Domain;

public sealed class MembershipAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public required string ActorEmail { get; set; }
    public required string TargetEmail { get; set; }
    public required string Action { get; set; }
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
