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
            e.Property(x => x.Tags).HasMaxLength(1000).HasDefaultValue(string.Empty);
            e.Property(x => x.Context).HasMaxLength(1000).HasDefaultValue(string.Empty);
            e.Property(x => x.Notes).HasMaxLength(2000).HasDefaultValue(string.Empty);
            e.HasIndex(x => new { x.ProjectId, x.Key }).IsUnique();
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
            e.Property(x => x.Status).HasMaxLength(32).HasDefaultValue("todo");
            e.HasIndex(x => new { x.ContentItemId, x.LanguageCode }).IsUnique();
            e.HasIndex(x => x.DueUtc);
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
        services.AddScoped<IContentItemService, ContentItemService>();

        return services;
    }
}
