using ContentLocalizationSaaS.Application.Abstractions;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContentLocalizationSaaS.Infrastructure;

public sealed class AppDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectAuditLog> ProjectAuditLogs => Set<ProjectAuditLog>();
    public DbSet<ProjectCollection> ProjectCollections => Set<ProjectCollection>();
    public DbSet<WorkspaceInvite> WorkspaceInvites => Set<WorkspaceInvite>();
    public DbSet<WorkspaceMembership> WorkspaceMemberships => Set<WorkspaceMembership>();
    public DbSet<MembershipAuditLog> MembershipAuditLogs => Set<MembershipAuditLog>();
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<CopyComponent> CopyComponents => Set<CopyComponent>();
    public DbSet<UsageReference> UsageReferences => Set<UsageReference>();
    public DbSet<SavedFilterPreset> SavedFilterPresets => Set<SavedFilterPreset>();
    public DbSet<ContentItemRevision> ContentItemRevisions => Set<ContentItemRevision>();
    public DbSet<ProjectLanguage> ProjectLanguages => Set<ProjectLanguage>();
    public DbSet<ContentItemLanguageTask> ContentItemLanguageTasks => Set<ContentItemLanguageTask>();
    public DbSet<TranslationMemoryEntry> TranslationMemoryEntries => Set<TranslationMemoryEntry>();
    public DbSet<DiscussionThread> DiscussionThreads => Set<DiscussionThread>();
    public DbSet<DiscussionComment> DiscussionComments => Set<DiscussionComment>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<ExternalReviewLink> ExternalReviewLinks => Set<ExternalReviewLink>();
    public DbSet<ActivityFeedEvent> ActivityFeedEvents => Set<ActivityFeedEvent>();
    public DbSet<PluginSession> PluginSessions => Set<PluginSession>();
    public DbSet<DesignLayerLink> DesignLayerLinks => Set<DesignLayerLink>();
    public DbSet<PluginSyncConflict> PluginSyncConflicts => Set<PluginSyncConflict>();
    public DbSet<ProjectKeyConvention> ProjectKeyConventions => Set<ProjectKeyConvention>();
    public DbSet<ApiToken> ApiTokens => Set<ApiToken>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs => Set<WebhookDeliveryLog>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<ContentReview> ContentReviews => Set<ContentReview>();
    public DbSet<ProjectVersion> ProjectVersions => Set<ProjectVersion>();
    public DbSet<ProjectVersionSnapshot> ProjectVersionSnapshots => Set<ProjectVersionSnapshot>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Workspace>(e =>
        {
            e.ToTable("workspaces");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        builder.Entity<Project>(e =>
        {
            e.ToTable("projects");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.SourceLanguage).HasMaxLength(16).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000).HasDefaultValue(string.Empty);
            e.HasIndex(x => new { x.WorkspaceId, x.Name });
        });

        builder.Entity<ProjectAuditLog>(e =>
        {
            e.ToTable("project_audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(64).IsRequired();
            e.Property(x => x.Details).HasMaxLength(4000).HasDefaultValue(string.Empty);
            e.HasIndex(x => new { x.ProjectId, x.CreatedUtc });
        });

        builder.Entity<ProjectCollection>(e =>
        {
            e.ToTable("project_collections");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.IsRoot).HasDefaultValue(false);
            e.HasIndex(x => new { x.ProjectId, x.ParentId, x.SortOrder });
            e.HasIndex(x => new { x.ProjectId, x.ParentId, x.Name }).IsUnique();
            e.HasIndex(x => new { x.ProjectId, x.IsRoot }).IsUnique().HasFilter("\"is_root\" = true");
        });

        builder.Entity<WorkspaceInvite>(e =>
        {
            e.ToTable("workspace_invites");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(320).IsRequired();
            e.Property(x => x.Role).HasMaxLength(32).IsRequired();
            e.Property(x => x.Token).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => new { x.WorkspaceId, x.Email, x.Status });
        });

        builder.Entity<WorkspaceMembership>(e =>
        {
            e.ToTable("workspace_memberships");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(320).IsRequired();
            e.Property(x => x.Role).HasMaxLength(32).IsRequired();
            e.HasIndex(x => new { x.WorkspaceId, x.Email }).IsUnique();
        });

        builder.Entity<MembershipAuditLog>(e =>
        {
            e.ToTable("membership_audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.ActorEmail).HasMaxLength(320).IsRequired();
            e.Property(x => x.TargetEmail).HasMaxLength(320).IsRequired();
            e.Property(x => x.Action).HasMaxLength(64).IsRequired();
            e.Property(x => x.OldValue).HasMaxLength(256).HasDefaultValue(string.Empty);
            e.Property(x => x.NewValue).HasMaxLength(256).HasDefaultValue(string.Empty);
            e.HasIndex(x => new { x.WorkspaceId, x.CreatedUtc });
            e.HasIndex(x => x.TargetEmail);
            e.HasIndex(x => x.Action);
        });

        builder.Entity<ContentItem>(e =>
        {
            e.ToTable("content_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(200).IsRequired();
            e.Property(x => x.Source).HasMaxLength(4000).IsRequired();
            e.Property(x => x.Status).HasMaxLength(32).IsRequired();
            e.Property(x => x.ReviewAssigneeEmail).HasMaxLength(320).HasDefaultValue(string.Empty);
            e.Property(x => x.ApprovedByEmail).HasMaxLength(320).HasDefaultValue(string.Empty);
            e.Property(x => x.RejectionReason).HasMaxLength(1000).HasDefaultValue(string.Empty);
            e.Property(x => x.Tags).HasMaxLength(1000).HasDefaultValue(string.Empty);
            e.Property(x => x.Context).HasMaxLength(1000).HasDefaultValue(string.Empty);
            e.Property(x => x.Notes).HasMaxLength(2000).HasDefaultValue(string.Empty);
            e.HasIndex(x => new { x.ProjectId, x.Key }).IsUnique();
            e.HasIndex(x => new { x.ProjectId, x.CollectionId });
            e.HasIndex(x => x.Tags);
            e.HasIndex(x => x.CopyComponentId);
        });

        builder.Entity<CopyComponent>(e =>
        {
            e.ToTable("copy_components");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Source).HasMaxLength(4000).IsRequired();
            e.HasIndex(x => new { x.ProjectId, x.Name }).IsUnique();
        });

        builder.Entity<UsageReference>(e =>
        {
            e.ToTable("usage_references");
            e.HasKey(x => x.Id);
            e.Property(x => x.Screen).HasMaxLength(200).HasDefaultValue(string.Empty);
            e.Property(x => x.Component).HasMaxLength(200).HasDefaultValue(string.Empty);
            e.Property(x => x.ReferencePath).HasMaxLength(500).HasDefaultValue(string.Empty);
            e.HasIndex(x => x.ContentItemId);
            e.HasIndex(x => new { x.ProjectId, x.Screen, x.Component });
        });

        builder.Entity<SavedFilterPreset>(e =>
        {
            e.ToTable("saved_filter_presets");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Query).HasMaxLength(500).HasDefaultValue(string.Empty);
            e.Property(x => x.Tags).HasMaxLength(500).HasDefaultValue(string.Empty);
            e.Property(x => x.Status).HasMaxLength(32).HasDefaultValue(string.Empty);
            e.HasIndex(x => new { x.ProjectId, x.Name }).IsUnique();
        });

        builder.Entity<ContentItemRevision>(e =>
        {
            e.ToTable("content_item_revisions");
            e.HasKey(x => x.Id);
            e.Property(x => x.ActorEmail).HasMaxLength(320).IsRequired();
            e.Property(x => x.PreviousSource).HasMaxLength(4000).HasDefaultValue(string.Empty);
            e.Property(x => x.NewSource).HasMaxLength(4000).HasDefaultValue(string.Empty);
            e.Property(x => x.PreviousStatus).HasMaxLength(32).HasDefaultValue(string.Empty);
            e.Property(x => x.NewStatus).HasMaxLength(32).HasDefaultValue(string.Empty);
            e.Property(x => x.DiffSummary).HasMaxLength(500).HasDefaultValue(string.Empty);
            e.Property(x => x.EventType).HasMaxLength(32).HasDefaultValue(string.Empty);
            e.HasIndex(x => new { x.ContentItemId, x.CreatedUtc });
        });

        builder.Entity<ProjectLanguage>(e =>
        {
            e.ToTable("project_languages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Bcp47Code).HasMaxLength(35).IsRequired();
            e.HasIndex(x => new { x.ProjectId, x.Bcp47Code }).IsUnique();
            e.HasIndex(x => new { x.ProjectId, x.IsActive });
        });

        builder.Entity<ContentItemLanguageTask>(e =>
        {
            e.ToTable("content_item_language_tasks");
            e.HasKey(x => x.Id);
            e.Property(x => x.LanguageCode).HasMaxLength(35).IsRequired();
            e.Property(x => x.AssigneeEmail).HasMaxLength(320).HasDefaultValue(string.Empty);
            e.Property(x => x.TranslationText).HasMaxLength(4000).HasDefaultValue(string.Empty);
            e.Property(x => x.PreviousApprovedTranslation).HasMaxLength(4000).HasDefaultValue(string.Empty);
            e.Property(x => x.Status).HasMaxLength(32).HasDefaultValue("todo");
            e.HasIndex(x => new { x.ContentItemId, x.LanguageCode }).IsUnique();
            e.HasIndex(x => x.DueUtc);
            e.HasIndex(x => x.IsOutdated);
        });

        builder.Entity<TranslationMemoryEntry>(e =>
        {
            e.ToTable("translation_memory_entries");
            e.HasKey(x => x.Id);
            e.Property(x => x.SourceText).HasMaxLength(4000).IsRequired();
            e.Property(x => x.LanguageCode).HasMaxLength(35).IsRequired();
            e.Property(x => x.TranslationText).HasMaxLength(4000).IsRequired();
            e.HasIndex(x => new { x.ProjectId, x.LanguageCode, x.SourceText, x.IsApproved });
        });

        builder.Entity<DiscussionThread>(e =>
        {
            e.ToTable("discussion_threads");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).HasDefaultValue(string.Empty);
            e.Property(x => x.CreatedByEmail).HasMaxLength(320).HasDefaultValue(string.Empty);
            e.HasIndex(x => new { x.ContentItemId, x.IsResolved });
        });

        builder.Entity<DiscussionComment>(e =>
        {
            e.ToTable("discussion_comments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Body).HasMaxLength(4000).IsRequired();
            e.Property(x => x.AuthorEmail).HasMaxLength(320).HasDefaultValue(string.Empty);
            e.HasIndex(x => new { x.ThreadId, x.CreatedUtc });
            e.HasIndex(x => x.ParentCommentId);
            e.HasIndex(x => x.ReviewId);
        });

        builder.Entity<NotificationPreference>(e =>
        {
            e.ToTable("notification_preferences");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserEmail).HasMaxLength(320).IsRequired();
            e.HasIndex(x => x.UserEmail).IsUnique();
        });

        builder.Entity<UserNotification>(e =>
        {
            e.ToTable("user_notifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserEmail).HasMaxLength(320).IsRequired();
            e.Property(x => x.Type).HasMaxLength(64).IsRequired();
            e.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            e.Property(x => x.ChannelUsed).HasMaxLength(32).HasDefaultValue("in_app");
            e.HasIndex(x => new { x.UserEmail, x.IsRead, x.CreatedUtc });
        });

        builder.Entity<ExternalReviewLink>(e =>
        {
            e.ToTable("external_review_links");
            e.HasKey(x => x.Id);
            e.Property(x => x.Token).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => new { x.ContentItemId, x.ExpiresUtc });
        });

        builder.Entity<ActivityFeedEvent>(e =>
        {
            e.ToTable("activity_feed_events");
            e.HasKey(x => x.Id);
            e.Property(x => x.EventType).HasMaxLength(64).IsRequired();
            e.Property(x => x.ActorEmail).HasMaxLength(320).HasDefaultValue(string.Empty);
            e.Property(x => x.Message).HasMaxLength(1000).HasDefaultValue(string.Empty);
            e.HasIndex(x => new { x.ProjectId, x.CreatedUtc });
            e.HasIndex(x => x.EventType);
            e.HasIndex(x => x.ActorEmail);
        });

        builder.Entity<PluginSession>(e =>
        {
            e.ToTable("plugin_sessions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Token).HasMaxLength(128).IsRequired();
            e.Property(x => x.UserEmail).HasMaxLength(320).IsRequired();
            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => new { x.UserEmail, x.ExpiresUtc });
        });

        builder.Entity<DesignLayerLink>(e =>
        {
            e.ToTable("design_layer_links");
            e.HasKey(x => x.Id);
            e.Property(x => x.DesignFileId).HasMaxLength(120).IsRequired();
            e.Property(x => x.LayerId).HasMaxLength(120).IsRequired();
            e.Property(x => x.DuplicateLinkRule).HasMaxLength(32).HasDefaultValue("preserve");
            e.HasIndex(x => new { x.ProjectId, x.DesignFileId, x.LayerId }).IsUnique();
            e.HasIndex(x => x.ContentItemId);
        });

        builder.Entity<PluginSyncConflict>(e =>
        {
            e.ToTable("plugin_sync_conflicts");
            e.HasKey(x => x.Id);
            e.Property(x => x.CurrentText).HasMaxLength(4000).HasDefaultValue(string.Empty);
            e.Property(x => x.ProposedText).HasMaxLength(4000).HasDefaultValue(string.Empty);
            e.Property(x => x.ResolutionState).HasMaxLength(32).HasDefaultValue("open");
            e.HasIndex(x => new { x.DesignLayerLinkId, x.ResolutionState, x.CreatedUtc });
        });

        builder.Entity<ProjectKeyConvention>(e =>
        {
            e.ToTable("project_key_conventions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Convention).HasMaxLength(32).HasDefaultValue("dot.case");
            e.Property(x => x.Prefix).HasMaxLength(64).HasDefaultValue(string.Empty);
            e.HasIndex(x => x.ProjectId).IsUnique();
        });

        builder.Entity<ApiToken>(e =>
        {
            e.ToTable("api_tokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            e.Property(x => x.Scope).HasMaxLength(128).HasDefaultValue(string.Empty);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => new { x.IsRevoked, x.ExpiresUtc });
        });

        builder.Entity<WebhookSubscription>(e =>
        {
            e.ToTable("webhook_subscriptions");
            e.HasKey(x => x.Id);
            e.Property(x => x.EndpointUrl).HasMaxLength(500).IsRequired();
            e.Property(x => x.Secret).HasMaxLength(128).IsRequired();
            e.HasIndex(x => new { x.ProjectId, x.IsActive });
        });

        builder.Entity<WebhookDeliveryLog>(e =>
        {
            e.ToTable("webhook_delivery_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.EventType).HasMaxLength(64).IsRequired();
            e.Property(x => x.PayloadJson).HasMaxLength(8000).HasDefaultValue(string.Empty);
            e.Property(x => x.IdempotencyKey).HasMaxLength(128).HasDefaultValue(string.Empty);
            e.Property(x => x.Status).HasMaxLength(32).HasDefaultValue("pending");
            e.Property(x => x.LastError).HasMaxLength(500).HasDefaultValue(string.Empty);
            e.HasIndex(x => new { x.SubscriptionId, x.Status, x.NextAttemptUtc });
            e.HasIndex(x => x.IdempotencyKey).IsUnique();
        });

        builder.Entity<IdempotencyRecord>(e =>
        {
            e.ToTable("idempotency_records");
            e.HasKey(x => x.Id);
            e.Property(x => x.Operation).HasMaxLength(64).IsRequired();
            e.Property(x => x.Key).HasMaxLength(128).IsRequired();
            e.Property(x => x.ResponseJson).HasMaxLength(8000).HasDefaultValue(string.Empty);
            e.Property(x => x.HitCount).HasDefaultValue(1);
            e.HasIndex(x => new { x.Operation, x.Key }).IsUnique();
        });

        builder.Entity<ContentReview>(e =>
        {
            e.ToTable("content_reviews");
            e.HasKey(x => x.Id);
            e.Property(x => x.ReviewerEmail).HasMaxLength(320).IsRequired();
            e.Property(x => x.Verdict).HasMaxLength(32).IsRequired();
            e.Property(x => x.Body).HasMaxLength(4000).HasDefaultValue(string.Empty);
            e.HasIndex(x => new { x.ContentItemId, x.CreatedUtc });
            e.HasIndex(x => x.ReviewerEmail);
        });

        builder.Entity<ProjectVersion>(e =>
        {
            e.ToTable("project_versions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Tag).HasMaxLength(128).IsRequired();
            e.Property(x => x.Title).HasMaxLength(256).HasDefaultValue(string.Empty);
            e.Property(x => x.Notes).HasMaxLength(8000).HasDefaultValue(string.Empty);
            e.Property(x => x.CreatedByEmail).HasMaxLength(320).HasDefaultValue(string.Empty);
            e.HasIndex(x => new { x.ProjectId, x.CreatedUtc });
            e.HasIndex(x => new { x.ProjectId, x.Tag }).IsUnique();
            e.HasIndex(x => new { x.ProjectId, x.IsLive }).IsUnique().HasFilter("\"is_live\" = true");
        });

        builder.Entity<ProjectVersionSnapshot>(e =>
        {
            e.ToTable("project_version_snapshots");
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(200).HasDefaultValue(string.Empty);
            e.Property(x => x.Source).HasMaxLength(4000).HasDefaultValue(string.Empty);
            e.Property(x => x.Status).HasMaxLength(32).HasDefaultValue(string.Empty);
            e.Property(x => x.Tags).HasMaxLength(1000).HasDefaultValue(string.Empty);
            e.Property(x => x.TranslationsJson).HasMaxLength(16000).HasDefaultValue(string.Empty);
            e.HasIndex(x => x.VersionId);
            e.HasIndex(x => new { x.VersionId, x.Key });
        });

        // Story 8.1: explicit FK constraints for core relations
        builder.Entity<Project>()
            .HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(x => x.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProjectAuditLog>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProjectCollection>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProjectCollection>()
            .HasOne<ProjectCollection>()
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WorkspaceInvite>()
            .HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(x => x.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WorkspaceMembership>()
            .HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(x => x.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ContentItem>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ContentItem>()
            .HasOne<CopyComponent>()
            .WithMany()
            .HasForeignKey(x => x.CopyComponentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<ContentItem>()
            .HasOne<ProjectCollection>()
            .WithMany()
            .HasForeignKey(x => x.CollectionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<CopyComponent>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UsageReference>()
            .HasOne<ContentItem>()
            .WithMany()
            .HasForeignKey(x => x.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ContentItemRevision>()
            .HasOne<ContentItem>()
            .WithMany()
            .HasForeignKey(x => x.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProjectLanguage>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ContentItemLanguageTask>()
            .HasOne<ContentItem>()
            .WithMany()
            .HasForeignKey(x => x.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<DiscussionThread>()
            .HasOne<ContentItem>()
            .WithMany()
            .HasForeignKey(x => x.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<DiscussionComment>()
            .HasOne<DiscussionThread>()
            .WithMany()
            .HasForeignKey(x => x.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ContentReview>()
            .HasOne<ContentItem>()
            .WithMany()
            .HasForeignKey(x => x.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<DiscussionComment>()
            .HasOne<ContentReview>()
            .WithMany()
            .HasForeignKey(x => x.ReviewId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<ExternalReviewLink>()
            .HasOne<ContentItem>()
            .WithMany()
            .HasForeignKey(x => x.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<DesignLayerLink>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<DesignLayerLink>()
            .HasOne<ContentItem>()
            .WithMany()
            .HasForeignKey(x => x.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PluginSyncConflict>()
            .HasOne<DesignLayerLink>()
            .WithMany()
            .HasForeignKey(x => x.DesignLayerLinkId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProjectKeyConvention>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WebhookSubscription>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WebhookDeliveryLog>()
            .HasOne<WebhookSubscription>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProjectVersion>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProjectVersionSnapshot>()
            .HasOne<ProjectVersion>()
            .WithMany()
            .HasForeignKey(x => x.VersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("contentdb")
            ?? configuration.GetConnectionString("postgres")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'contentdb', 'postgres', or 'DefaultConnection' is required.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services
            .AddIdentityCore<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<IWorkspaceService, WorkspaceService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IProjectCollectionService, ProjectCollectionService>();
        services.AddScoped<IContentItemService, ContentItemService>();

        return services;
    }
}
