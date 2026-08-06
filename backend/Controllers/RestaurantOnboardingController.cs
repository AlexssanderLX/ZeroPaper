using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using ZeroPaper.DTOs.Onboarding;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Controllers;

[ApiController]
[AllowAnonymous]
[RequestSizeLimit(64 * 1024)]
[Route("api/onboarding/restaurants")]
[Route("api/onboarding")]
[EnableRateLimiting("public-write")]
public class RestaurantOnboardingController : ControllerBase
{
    private readonly IRestaurantOnboardingService _restaurantOnboardingService;

    public RestaurantOnboardingController(IRestaurantOnboardingService restaurantOnboardingService)
    {
        _restaurantOnboardingService = restaurantOnboardingService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(RestaurantOnboardingResponseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRestaurantAsync(
        [FromBody] RestaurantOnboardingRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _restaurantOnboardingService.CreateAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Cadastro invalido",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Pagamento indisponivel",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }
}
