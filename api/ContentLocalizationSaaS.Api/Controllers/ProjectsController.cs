using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Application;
using ContentLocalizationSaaS.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Microsoft.AspNetCore.Cors.EnableCors("PluginCors")]
public sealed class ProjectsController(IProjectService projects) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? workspaceId, CancellationToken cancellationToken)
    {
        var items = await projects.GetAllAsync(workspaceId, cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await projects.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var project = await projects.GetByIdAsync(id, cancellationToken);
        return Ok(project);
    }

    [HttpPut("{id:guid}")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await projects.UpdateAsync(id, request, cancellationToken);
        return Ok(project);
    }

    [HttpGet("{id:guid}/audit-logs")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> GetAuditLogs(Guid id, CancellationToken cancellationToken)
    {
        var logs = await projects.GetAuditLogsAsync(id, cancellationToken);
        return Ok(logs);
    }
}
