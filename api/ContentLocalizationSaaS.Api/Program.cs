using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Api.Middleware;
using ContentLocalizationSaaS.Application;
using ContentLocalizationSaaS.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ProblemDetailsResultFilter>();
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddValidatorsFromAssemblyContaining<CreateProjectRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateContentItemRequestValidator>();

builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = authOptions.Oidc.RequireHttpsMetadata;
        options.Authority = authOptions.Oidc.Issuer;
        options.MapInboundClaims = false;
        options.TokenValidationParameters.ValidateIssuer = true;
        options.TokenValidationParameters.ValidIssuer = authOptions.Oidc.Issuer;
        options.TokenValidationParameters.ValidateAudience = false;
        options.TokenValidationParameters.ValidateLifetime = true;

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var principal = context.Principal;
                var expectedAudience = authOptions.Oidc.Audience;

                var audClaims = principal?.FindAll("aud").Select(c => c.Value) ?? Enumerable.Empty<string>();
                var azp = principal?.FindFirst("azp")?.Value;

                var audienceMatch = audClaims.Contains(expectedAudience, StringComparer.OrdinalIgnoreCase)
                                    || string.Equals(azp, expectedAudience, StringComparison.OrdinalIgnoreCase);

                if (!audienceMatch)
                {
                    context.Fail($"Token audience/azp does not match expected client '{expectedAudience}'.");
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDevCors", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                "http://127.0.0.1:3000",
                "https://127.0.0.1:3000",
                "http://localhost:5173",
                "https://localhost:5173",
                "http://127.0.0.1:5173",
                "https://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

var isDevelopment = app.Environment.IsDevelopment();
var allowsDebugUserDeletion = DebugEndpointEnvironmentPolicy.AllowsDebugUserDeletion(app.Environment.EnvironmentName);

if (isDevelopment || allowsDebugUserDeletion)
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "ContentLocalizationSaaS API";
    });
}

if (isDevelopment)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup.Migrations");
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    var pendingBefore = (await db.Database.GetPendingMigrationsAsync()).ToArray();
    if (pendingBefore.Length > 0)
    {
        logger.LogInformation("Applying {Count} pending migrations: {Migrations}", pendingBefore.Length, string.Join(", ", pendingBefore));
        await db.Database.MigrateAsync();
    }

    var pendingAfter = (await db.Database.GetPendingMigrationsAsync()).ToArray();
    if (pendingAfter.Length > 0)
    {
        throw new InvalidOperationException($"Pending migrations remain after startup migration step: {string.Join(", ", pendingAfter)}");
    }

    foreach (var appRole in Enum.GetNames<AppRole>())
    {
        if (await roleManager.RoleExistsAsync(appRole))
        {
            continue;
        }

        var createRoleResult = await roleManager.CreateAsync(new IdentityRole(appRole));
        if (!createRoleResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create role '{appRole}': {string.Join(", ", createRoleResult.Errors.Select(e => e.Description))}");
        }

        logger.LogInformation("Created missing identity role: {Role}", appRole);
    }
}

app.MapDefaultEndpoints();
app.UseObservabilityMiddleware();
app.UseApiExceptionMiddleware();
app.UseHttpsRedirection();

if (isDevelopment)
{
    app.UseCors("LocalDevCors");
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (!DebugEndpointEnvironmentPolicy.IsProduction(app.Environment.EnvironmentName))
{
    app.MapDelete("/debug/users", async (
            IHostEnvironment environment,
            UserManager<IdentityUser> userManager,
            [AsParameters] DeleteDebugUserRequest request,
            CancellationToken cancellationToken) =>
        {
            if (!DebugEndpointEnvironmentPolicy.AllowsDebugUserDeletion(environment.EnvironmentName))
            {
                return Results.Json(new
                {
                    deleted = false,
                    reason = DebugEndpointEnvironmentPolicy.GetDebugUserDeletionDeniedReason(environment.EnvironmentName),
                    environment = environment.EnvironmentName
                }, statusCode: StatusCodes.Status403Forbidden);
            }

            if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Id))
            {
                return Results.BadRequest(new
                {
                    deleted = false,
                    reason = "email_or_id_required"
                });
            }

            IdentityUser? user = null;
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                user = await userManager.FindByEmailAsync(request.Email.Trim());
            }

            if (user is null && !string.IsNullOrWhiteSpace(request.Id))
            {
                user = await userManager.FindByIdAsync(request.Id.Trim());
            }

            if (user is null)
            {
                return Results.NotFound(new
                {
                    deleted = false,
                    reason = "user_not_found",
                    requestedEmail = request.Email,
                    requestedId = request.Id
                });
            }

            var deleteResult = await userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                return Results.Json(new
                {
                    deleted = false,
                    reason = "delete_failed",
                    errors = deleteResult.Errors.Select(e => e.Description)
                }, statusCode: StatusCodes.Status500InternalServerError);
            }

            return Results.Ok(new
            {
                deleted = true,
                userId = user.Id,
                email = user.Email,
                reason = "deleted"
            });
        })
        .WithTags("Debug")
        .WithOpenApi();
}

app.MapGet("/healthz", () => Results.Ok(new { status = "ok", service = "content-localization-api" }));

app.Run();

public sealed record DeleteDebugUserRequest(string? Email, string? Id);

public partial class Program { }
