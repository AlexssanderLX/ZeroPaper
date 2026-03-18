using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/public/tables")]
public class PublicOrderingController : ControllerBase
{
    private readonly IWorkspaceService _workspaceService;

    public PublicOrderingController(IWorkspaceService workspaceService)
    {
        _workspaceService = workspaceService;
    }

    [HttpGet("{publicCode}")]
    [ProducesResponseType(typeof(PublicTableViewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTableAsync(string publicCode, CancellationToken cancellationToken)
    {
        return Ok(await _workspaceService.GetPublicTableAsync(publicCode, cancellationToken));
    }

    [HttpPost("{publicCode}/orders")]
    [EnableRateLimiting("public-write")]
    [ProducesResponseType(typeof(CustomerOrderDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOrderAsync(string publicCode, [FromBody] CreateCustomerOrderRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _workspaceService.CreatePublicOrderAsync(publicCode, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
