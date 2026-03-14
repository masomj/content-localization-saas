namespace ContentLocalizationSaaS.Domain;

public sealed class Workspace
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public required string Name { get; set; }
    public required string SourceLanguage { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ProjectAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public required string Action { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public enum InviteStatus
{
    Pending = 0,
    Accepted = 1,
    Expired = 2,
    Revoked = 3
}

public sealed class WorkspaceInvite
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
    public required string Token { get; set; }
    public InviteStatus Status { get; set; } = InviteStatus.Pending;
    public DateTime ExpiresUtc { get; set; } = DateTime.UtcNow.AddDays(7);
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class WorkspaceMembership
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ContentItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid? CopyComponentId { get; set; }
    public required string Key { get; set; }
    public required string Source { get; set; }
    public required string Status { get; set; }
    public string Tags { get; set; } = string.Empty; // pipe-delimited for MVP
    public string Context { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class CopyComponent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public required string Name { get; set; }
    public required string Source { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class UsageReference
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContentItemId { get; set; }
    public Guid ProjectId { get; set; }
    public string Screen { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public string ReferencePath { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ContentItemRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContentItemId { get; set; }
    public required string ActorEmail { get; set; }
    public string PreviousSource { get; set; } = string.Empty;
    public string NewSource { get; set; } = string.Empty;
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string DiffSummary { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // edited / rollback
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ProjectLanguage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public required string Bcp47Code { get; set; }
    public bool IsSource { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ContentItemLanguageTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContentItemId { get; set; }
    public required string LanguageCode { get; set; }
    public string AssigneeEmail { get; set; } = string.Empty;
    public string TranslationText { get; set; } = string.Empty;
    public string PreviousApprovedTranslation { get; set; } = string.Empty;
    public bool IsOutdated { get; set; }
    public DateTime? DueUtc { get; set; }
    public string Status { get; set; } = "todo";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class TranslationMemoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public required string SourceText { get; set; }
    public required string LanguageCode { get; set; }
    public required string TranslationText { get; set; }
    public bool IsApproved { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class DiscussionThread
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContentItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CreatedByEmail { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedUtc { get; set; }
}

public sealed class DiscussionComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ThreadId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public string Body { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}