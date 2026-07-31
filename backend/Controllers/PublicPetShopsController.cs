using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/public/petshops")]
public sealed class PublicPetShopsController : ControllerBase
{
    private readonly IPublicPetShopService _service;
    public PublicPetShopsController(IPublicPetShopService service) => _service = service;

    [HttpGet("{publicCode}")]
    public async Task<IActionResult> GetAsync(string publicCode, CancellationToken cancellationToken) => Ok(await _service.GetAsync(publicCode, cancellationToken));

    [HttpGet("{publicCode}/services")]
    public async Task<IActionResult> GetServicesAsync(string publicCode, CancellationToken cancellationToken) => Ok(await _service.GetServicesAsync(publicCode, cancellationToken));

    [HttpGet("{publicCode}/availability")]
    public async Task<IActionResult> GetAvailabilityAsync(string publicCode, [FromQuery] DateOnly date, [FromQuery] Guid serviceId, CancellationToken cancellationToken)
        => Ok(await _service.GetAvailabilityAsync(publicCode, date, serviceId, cancellationToken));

    [HttpPost("{publicCode}/appointment-requests")]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> CreateAsync(string publicCode, [FromBody] PublicAppointmentRequestDto request, CancellationToken cancellationToken)
        => Ok(await _service.CreateRequestAsync(publicCode, request, cancellationToken));

    [HttpGet("appointments/{accessToken}")]
    public async Task<IActionResult> TrackAsync(string accessToken, CancellationToken cancellationToken) => Ok(await _service.GetTrackingAsync(accessToken, cancellationToken));

    [HttpPost("appointments/{accessToken}/cancel")]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> CancelAsync(string accessToken, CancellationToken cancellationToken) => Ok(await _service.CancelAsync(accessToken, cancellationToken));
}
