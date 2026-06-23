using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZeroPaper.DTOs.Admin;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/admin/owners")]
public class AdminOwnersController : ControllerBase
{
    private readonly IAuthSessionService _authSessionService;
    private readonly IAdminOwnerService _adminOwnerService;

    public AdminOwnersController(IAuthSessionService authSessionService, IAdminOwnerService adminOwnerService)
    {
        _authSessionService = authSessionService;
        _adminOwnerService = adminOwnerService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AdminOwnerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOwnersAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _adminOwnerService.GetOwnersAsync(session, cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("{ownerId:guid}")]
    [ProducesResponseType(typeof(AdminOwnerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOwnerByIdAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        try
        {
            var owner = await _adminOwnerService.GetOwnerByIdAsync(session, ownerId, cancellationToken);
            return owner is null ? NotFound() : Ok(owner);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(typeof(AdminOwnerDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOwnerAsync(
        [FromBody] CreateAdminOwnerRequestDto request,
        CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        try
        {
            var owner = await _adminOwnerService.CreateOwnerAsync(session, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, owner);
        }
        catch (Exception exception) when (IsBadRequestException(exception))
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut("{ownerId:guid}")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(typeof(AdminOwnerDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateOwnerAsync(
        Guid ownerId,
        [FromBody] UpdateAdminOwnerRequestDto request,
        CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _adminOwnerService.UpdateOwnerAsync(session, ownerId, request, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails { Detail = exception.Message });
        }
        catch (Exception exception) when (IsBadRequestException(exception))
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPatch("{ownerId:guid}/deactivate")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(typeof(AdminOwnerDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeactivateOwnerAsync(
        Guid ownerId,
        [FromBody] ChangeAdminOwnerStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _adminOwnerService.DeactivateOwnerAsync(session, ownerId, request, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails { Detail = exception.Message });
        }
        catch (Exception exception) when (IsBadRequestException(exception))
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPatch("{ownerId:guid}/reactivate")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(typeof(AdminOwnerDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReactivateOwnerAsync(
        Guid ownerId,
        [FromBody] ChangeAdminOwnerStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _adminOwnerService.ReactivateOwnerAsync(session, ownerId, request, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails { Detail = exception.Message });
        }
        catch (Exception exception) when (IsBadRequestException(exception))
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{ownerId:guid}/reset-password")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetOwnerPasswordAsync(
        Guid ownerId,
        [FromBody] ResetAdminOwnerPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        try
        {
            await _adminOwnerService.ResetOwnerPasswordAsync(session, ownerId, request, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails { Detail = exception.Message });
        }
        catch (Exception exception) when (IsBadRequestException(exception))
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{ownerId:guid}")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> HardDeleteOwnerAsync(
        Guid ownerId,
        [FromBody] HardDeleteAdminOwnerRequestDto request,
        CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        try
        {
            await _adminOwnerService.HardDeleteOwnerAsync(session, ownerId, request, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails { Detail = exception.Message });
        }
        catch (Exception exception) when (IsBadRequestException(exception))
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private async Task<WorkspaceSessionContext?> GetRequiredSessionAsync(CancellationToken cancellationToken)
    {
        return await _authSessionService.GetSessionAsync(Request.Headers.Authorization.ToString(), cancellationToken);
    }

    private static bool IsBadRequestException(Exception exception)
    {
        return exception is ArgumentException or InvalidOperationException;
    }
}
