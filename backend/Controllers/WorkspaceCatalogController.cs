using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ZeroPaper.Security;
using ZeroPaper.Domain.Enums;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Controllers;

[ApiController]
[Authorize(Policy = ZeroPaperSecurity.WorkspacePolicy)]
[Route("api/workspace/catalog")]
public sealed class WorkspaceCatalogController : ControllerBase
{
    private readonly IAuthSessionService _auth;
    private readonly IWorkspaceService _workspace;
    public WorkspaceCatalogController(IAuthSessionService auth, IWorkspaceService workspace) { _auth = auth; _workspace = workspace; }

    [HttpGet("items")]
    public async Task<IActionResult> GetItemsAsync([FromQuery] CatalogItemKind? kind, CancellationToken cancellationToken)
    {
        var session = await _auth.GetSessionAsync(Request.Headers.Authorization.ToString(), cancellationToken);
        if (session is null) return Unauthorized();
        if (!session.Capabilities.HasCatalog) return Forbid();
        return Ok(await _workspace.GetCatalogItemsAsync(session, kind, cancellationToken));
    }
}
