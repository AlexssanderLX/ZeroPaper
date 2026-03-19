using Microsoft.AspNetCore.Mvc;
using ZeroPaper.DTOs.Admin;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IAuthSessionService _authSessionService;
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IAuthSessionService authSessionService, IAdminUserService adminUserService)
    {
        _authSessionService = authSessionService;
        _adminUserService = adminUserService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AdminUserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsersAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _adminUserService.GetUsersAsync(session, cancellationToken));
    }

    [HttpPatch("{userId:guid}/deactivate")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _adminUserService.DeactivateUserAsync(session, userId, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails { Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
    }

    [HttpPatch("{userId:guid}/reactivate")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReactivateUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _adminUserService.ReactivateUserAsync(session, userId, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails { Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
    }

    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        try
        {
            await _adminUserService.DeleteUserAsync(session, userId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails { Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
    }

    private async Task<WorkspaceSessionContext?> GetRequiredSessionAsync(CancellationToken cancellationToken)
    {
        return await _authSessionService.GetSessionAsync(Request.Headers.Authorization.ToString(), cancellationToken);
    }
}
