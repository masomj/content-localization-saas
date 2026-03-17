using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<IdentityUser> userManager,
    IConfiguration configuration,
    AppDbContext dbContext,
    IWebHostEnvironment environment,
    ILogger<AuthController> logger) : ControllerBase
{
    private readonly JwtOptions _jwtOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()?.Jwt 
        ?? throw new InvalidOperationException("JWT configuration is missing");

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { error = "A user with this email already exists" });
        }

        var isFirstUser = !await dbContext.Users.AnyAsync(cancellationToken);

        var user = new IdentityUser
        {
            Email = request.Email,
            UserName = request.Email,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(new { errors });
        }

        var role = isFirstUser ? AppRole.Admin : AppRole.Viewer;

        await userManager.AddToRoleAsync(user, role.ToString());
        
        var claims = new List<Claim>
        {
            new("app_role", role.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? ""),
            new("first_name", request.FirstName),
            new("last_name", request.LastName),
        };
        
        await userManager.AddClaimsAsync(user, claims);

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = request.Company ?? $"{request.FirstName}'s Workspace",
        };
        
        dbContext.Workspaces.Add(workspace);

        var membership = new WorkspaceMembership
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            Email = user.Email ?? "",
            Role = role.ToString(),
        };
        
        dbContext.WorkspaceMemberships.Add(membership);
        await dbContext.SaveChangesAsync(cancellationToken);

        var token = GenerateJwtToken(user, role.ToString(), request.FirstName, request.LastName);

        return Ok(new AuthResponse
        {
            Token = token,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email ?? "",
                Name = $"{request.FirstName} {request.LastName}",
                Role = role.ToString(),
            },
            Workspace = new WorkspaceInfo
            {
                Id = workspace.Id,
                Name = workspace.Name,
            }
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { error = "Invalid email or password" });
        }

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? AppRole.Viewer.ToString();

        var anyAdmins = await userManager.GetUsersInRoleAsync(AppRole.Admin.ToString());
        if (anyAdmins.Count == 0)
        {
            if (!roles.Contains(AppRole.Admin.ToString()))
            {
                await userManager.AddToRoleAsync(user, AppRole.Admin.ToString());
            }

            var existingClaims = await userManager.GetClaimsAsync(user);
            foreach (var existing in existingClaims.Where(c => c.Type == "app_role"))
            {
                await userManager.RemoveClaimAsync(user, existing);
            }
            await userManager.AddClaimAsync(user, new Claim("app_role", AppRole.Admin.ToString()));

            roles = await userManager.GetRolesAsync(user);
            role = roles.FirstOrDefault() ?? AppRole.Admin.ToString();
        }

        var claims = await userManager.GetClaimsAsync(user);
        var appRoleClaim = claims.FirstOrDefault(c => c.Type == "app_role")?.Value ?? role;
        var firstName = claims.FirstOrDefault(c => c.Type == "first_name")?.Value ?? "";
        var lastName = claims.FirstOrDefault(c => c.Type == "last_name")?.Value ?? "";

        var membership = await dbContext.WorkspaceMemberships
            .FirstOrDefaultAsync(m => m.Email == user.Email, cancellationToken);

        var workspace = membership != null 
            ? await dbContext.Workspaces.FirstOrDefaultAsync(w => w.Id == membership.WorkspaceId, cancellationToken)
            : null;

        var token = GenerateJwtToken(user, appRoleClaim, firstName, lastName);

        return Ok(new AuthResponse
        {
            Token = token,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email ?? "",
                Name = string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName)
                    ? user.Email ?? ""
                    : $"{firstName} {lastName}".Trim(),
                Role = appRoleClaim,
            },
            Workspace = workspace != null ? new WorkspaceInfo
            {
                Id = workspace.Id,
                Name = workspace.Name,
            } : null
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? AppRole.Viewer.ToString();

        var claims = await userManager.GetClaimsAsync(user);
        var appRoleClaim = claims.FirstOrDefault(c => c.Type == "app_role")?.Value ?? role;
        var firstName = claims.FirstOrDefault(c => c.Type == "first_name")?.Value ?? "";
        var lastName = claims.FirstOrDefault(c => c.Type == "last_name")?.Value ?? "";

        var membership = await dbContext.WorkspaceMemberships
            .FirstOrDefaultAsync(m => m.Email == user.Email, cancellationToken);

        var workspace = membership != null 
            ? await dbContext.Workspaces.FirstOrDefaultAsync(w => w.Id == membership.WorkspaceId, cancellationToken)
            : null;

        return Ok(new UserInfo
        {
            Id = user.Id,
            Email = user.Email ?? "",
            Name = string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName)
                ? user.Email ?? ""
                : $"{firstName} {lastName}".Trim(),
            Role = appRoleClaim,
            Workspace = workspace != null ? new WorkspaceInfo
            {
                Id = workspace.Id,
                Name = workspace.Name,
            } : null
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? AppRole.Viewer.ToString();

        var claims = await userManager.GetClaimsAsync(user);
        var appRoleClaim = claims.FirstOrDefault(c => c.Type == "app_role")?.Value ?? role;
        var firstName = claims.FirstOrDefault(c => c.Type == "first_name")?.Value ?? "";
        var lastName = claims.FirstOrDefault(c => c.Type == "last_name")?.Value ?? "";

        var token = GenerateJwtToken(user, appRoleClaim, firstName, lastName);

        return Ok(new { token });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { error = "Email is required" });
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Ok(new { message = "If an account exists for this email, a reset link has been sent." });
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var resetLink = $"/reset-password?email={Uri.EscapeDataString(user.Email ?? request.Email)}&token={encodedToken}";

        logger.LogInformation("Password reset requested for {Email}. Reset link: {ResetLink}", user.Email, resetLink);

        if (environment.IsDevelopment())
        {
            return Ok(new
            {
                message = "Password reset generated. In production this is sent via email.",
                resetLink,
            });
        }

        return Ok(new { message = "If an account exists for this email, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { error = "Email, token, and newPassword are required" });
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return BadRequest(new { error = "Invalid reset request" });
        }

        var decodedToken = Uri.UnescapeDataString(request.Token);
        var result = await userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        return Ok(new { message = "Password has been reset successfully" });
    }

    private string GenerateJwtToken(IdentityUser user, string role, string firstName, string lastName)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(ClaimTypes.Name, string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName)
                ? user.Email ?? ""
                : $"{firstName} {lastName}".Trim()),
            new Claim(ClaimTypes.Role, role),
            new Claim("app_role", role),
            new Claim("first_name", firstName ?? ""),
            new Claim("last_name", lastName ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed class RegisterRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Company { get; set; }
}

public sealed class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class ForgotPasswordRequest
{
    public string Email { get; set; } = "";
}

public sealed class ResetPasswordRequest
{
    public string Email { get; set; } = "";
    public string Token { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

public sealed class AuthResponse
{
    public string Token { get; set; } = "";
    public UserInfo User { get; set; } = new();
    public WorkspaceInfo? Workspace { get; set; }
}

public sealed class UserInfo
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public WorkspaceInfo? Workspace { get; set; }
}

public sealed class WorkspaceInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}
