using ContentLocalizationSaaS.Api.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/cli")]
public sealed class CliSpecController : ControllerBase
{
    [HttpGet("spec")]
    [RequireAppRole(AppRole.Viewer)]
    public IActionResult Spec()
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Generated-At-Utc"] = DateTime.UtcNow.ToString("O");

        return Ok(new
        {
            version = "v1",
            package = "content-localization-cli",
            commands = new object[]
            {
                new
                {
                    name = "configure",
                    description = "Save base URL + API token for non-interactive usage",
                    args = new[] { "--base-url", "--api-token" }
                },
                new
                {
                    name = "pull",
                    description = "Export approved content bundle",
                    args = new[] { "--project-id", "--language?", "--namespace?", "--out?" }
                }
            },
            env = new[] { "CLSAAS_BASE_URL", "CLSAAS_API_TOKEN" },
            exitCodes = new[]
            {
                new { code = 0, meaning = "success" },
                new { code = 2, meaning = "usage_or_config_error" },
                new { code = 10, meaning = "unauthorized" },
                new { code = 11, meaning = "forbidden" },
                new { code = 12, meaning = "not_found" },
                new { code = 13, meaning = "rate_limited" },
                new { code = 20, meaning = "server_error" },
                new { code = 21, meaning = "network_error" }
            }
        });
    }
}
