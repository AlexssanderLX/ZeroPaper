using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZeroPaper.DTOs.Public;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/public/access-requests")]
public class PublicAccessRequestsController : ControllerBase
{
    private readonly IAccessRequestNotificationService _accessRequestNotificationService;

    public PublicAccessRequestsController(IAccessRequestNotificationService accessRequestNotificationService)
    {
        _accessRequestNotificationService = accessRequestNotificationService;
    }

    [HttpPost]
    [EnableRateLimiting("public-write")]
    [ProducesResponseType(typeof(AccessRequestResponseDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> CreateAsync([FromBody] AccessRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _accessRequestNotificationService.SendAsync(request, cancellationToken);
        return Accepted(response);
    }
}
