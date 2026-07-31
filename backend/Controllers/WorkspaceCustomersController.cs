using Microsoft.AspNetCore.Mvc;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/workspace/customers")]
public sealed class WorkspaceCustomersController : ControllerBase
{
    private readonly IAuthSessionService _auth;
    private readonly ICustomerProfileService _customers;

    public WorkspaceCustomersController(IAuthSessionService auth, ICustomerProfileService customers)
    {
        _auth = auth;
        _customers = customers;
    }

    [HttpGet]
    public async Task<IActionResult> SearchAsync([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        var session = await SessionAsync(cancellationToken);
        if (session is null) return Unauthorized();
        if (!session.Capabilities.HasCustomerProfiles) return Forbid();
        return Ok(await _customers.SearchAsync(session, search, page, pageSize, cancellationToken));
    }

    [HttpGet("{customerId:guid}")]
    public async Task<IActionResult> GetAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var session = await SessionAsync(cancellationToken);
        if (session is null) return Unauthorized();
        if (!session.Capabilities.HasCustomerProfiles) return Forbid();
        return Ok(await _customers.GetByIdAsync(session, customerId, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateCustomerProfileRequestDto request, CancellationToken cancellationToken)
    {
        var session = await SessionAsync(cancellationToken);
        if (session is null) return Unauthorized();
        if (!session.Capabilities.HasCustomerProfiles) return Forbid();
        var created = await _customers.CreateAsync(session, request, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { customerId = created.Id }, created);
    }

    [HttpPut("{customerId:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid customerId, [FromBody] UpdateCustomerProfileRequestDto request, CancellationToken cancellationToken)
    {
        var session = await SessionAsync(cancellationToken);
        if (session is null) return Unauthorized();
        if (!session.Capabilities.HasCustomerProfiles) return Forbid();
        return Ok(await _customers.UpdateAsync(session, customerId, request, cancellationToken));
    }

    private Task<WorkspaceSessionContext?> SessionAsync(CancellationToken cancellationToken) =>
        _auth.GetSessionAsync(Request.Headers.Authorization.ToString(), cancellationToken);
}
