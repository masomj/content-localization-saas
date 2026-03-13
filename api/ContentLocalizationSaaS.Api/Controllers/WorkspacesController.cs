using ContentLocalizationSaaS.Application;
using ContentLocalizationSaaS.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/workspaces")]
public sealed class WorkspacesController(IWorkspaceService workspaces) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var items = await workspaces.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var workspace = await workspaces.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = workspace.Id }, workspace);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var workspace = await workspaces.GetByIdAsync(id, cancellationToken);
        return Ok(workspace);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var workspace = await workspaces.UpdateAsync(id, request, cancellationToken);
        return Ok(workspace);
    }
}
