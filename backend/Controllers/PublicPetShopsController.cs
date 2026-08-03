using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Controllers;

[ApiController]
[AllowAnonymous]
[RequestSizeLimit(64 * 1024)]
[Route("api/public/petshops")]
public sealed class PublicPetShopsController : ControllerBase
{
    private readonly IPublicPetShopService _service;
    private readonly IConfiguration _configuration;

    public PublicPetShopsController(IPublicPetShopService service, IConfiguration configuration)
    {
        _service = service;
        _configuration = configuration;
    }

    [HttpGet("{publicCode}")]
    public async Task<IActionResult> GetAsync(string publicCode, CancellationToken cancellationToken)
        => IsAvailable() ? Ok(await _service.GetAsync(publicCode, cancellationToken)) : NotFound();

    [HttpGet("{publicCode}/services")]
    public async Task<IActionResult> GetServicesAsync(string publicCode, CancellationToken cancellationToken)
        => IsAvailable() ? Ok(await _service.GetServicesAsync(publicCode, cancellationToken)) : NotFound();

    [HttpGet("{publicCode}/availability")]
    public async Task<IActionResult> GetAvailabilityAsync(string publicCode, [FromQuery] DateOnly date, [FromQuery] Guid serviceId, CancellationToken cancellationToken)
        => IsAvailable() ? Ok(await _service.GetAvailabilityAsync(publicCode, date, serviceId, cancellationToken)) : NotFound();

    [HttpPost("{publicCode}/appointment-requests")]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> CreateAsync(string publicCode, [FromBody] PublicAppointmentRequestDto request, CancellationToken cancellationToken)
        => IsAvailable() ? Ok(await _service.CreateRequestAsync(publicCode, request, cancellationToken)) : NotFound();

    [HttpGet("appointments/track")]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> TrackAsync([FromHeader(Name = "X-ZP-Public-Token")] string accessToken, CancellationToken cancellationToken)
        => IsAvailable() ? Ok(await _service.GetTrackingAsync(accessToken, cancellationToken)) : NotFound();

    [HttpPost("appointments/cancel")]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> CancelAsync([FromHeader(Name = "X-ZP-Public-Token")] string accessToken, CancellationToken cancellationToken)
        => IsAvailable() ? Ok(await _service.CancelAsync(accessToken, cancellationToken)) : NotFound();

    private bool IsAvailable() => _configuration.GetValue<bool>("Segments:petshop:Available");
}
