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
        try
        {
            var response = await _accessRequestNotificationService.SendAsync(request, cancellationToken);
            return Accepted(response);
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Title = "Envio indisponivel",
                Detail = exception.Message,
                Status = StatusCodes.Status503ServiceUnavailable
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Dados invalidos",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}
