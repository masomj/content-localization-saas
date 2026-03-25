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

public sealed class ProjectCollection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid? ParentId { get; set; }
    public required string Name { get; set; }
    public bool IsRoot { get; set; }
    public int Depth { get; set; }
    public int SortOrder { get; set; }
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
    public Guid? CollectionId { get; set; }
    public Guid? CopyComponentId { get; set; }
    public required string Key { get; set; }
    public required string Source { get; set; }
    public required string Status { get; set; }
    public int SortOrder { get; set; }
    public string ReviewAssigneeEmail { get; set; } = string.Empty;
    public DateTime? ApprovedUtc { get; set; }
    public string ApprovedByEmail { get; set; } = string.Empty;
    public string RejectionReason { get; set; } = string.Empty;
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
    public Guid? ReviewId { get; set; }
    public string Body { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class NotificationPreference
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserEmail { get; set; } = string.Empty;
    public bool InAppEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; }
    public bool SlackEnabled { get; set; }
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class UserNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserEmail { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ChannelUsed { get; set; } = "in_app";
    public bool IsRead { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ExternalReviewLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContentItemId { get; set; }
    public string Token { get; set; } = string.Empty;
    public bool CommentEnabled { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ActivityFeedEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string ActorEmail { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class PluginSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Token { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public Guid WorkspaceId { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ProjectKeyConvention
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Convention { get; set; } = "dot.case"; // dot.case | snake_case | kebab-case
    public string Prefix { get; set; } = string.Empty;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ApiToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public bool IsRevoked { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresUtc { get; set; } = DateTime.UtcNow.AddDays(90);
    public DateTime? LastUsedUtc { get; set; }
    public DateTime? RevokedUtc { get; set; }
}
public sealed class WebhookSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string EndpointUrl { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class WebhookDeliveryLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubscriptionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public int AttemptCount { get; set; } = 0;
    public string Status { get; set; } = "pending"; // pending|delivered|failed|dead_letter
    public DateTime? NextAttemptUtc { get; set; }
    public string LastError { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredUtc { get; set; }
}

public sealed class IdempotencyRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Operation { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string ResponseJson { get; set; } = string.Empty;
    public int HitCount { get; set; } = 1;
    public DateTime FirstSeenUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class DesignLayerLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string DesignFileId { get; set; } = string.Empty;
    public string LayerId { get; set; } = string.Empty;
    public Guid ContentItemId { get; set; }
    public string DuplicateLinkRule { get; set; } = "preserve"; // preserve|clear
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ContentReview
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContentItemId { get; set; }
    public string ReviewerEmail { get; set; } = string.Empty;
    public string Verdict { get; set; } = string.Empty; // approved | changes_requested | comment
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class PluginSyncConflict
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DesignLayerLinkId { get; set; }
    public string CurrentText { get; set; } = string.Empty;
    public string ProposedText { get; set; } = string.Empty;
    public string ResolutionState { get; set; } = "open";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}