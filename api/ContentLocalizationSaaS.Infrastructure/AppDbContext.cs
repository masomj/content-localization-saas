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

        return services;
    }
}
