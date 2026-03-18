using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZeroPaper.DTOs.Onboarding;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/onboarding/restaurants")]
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
        var response = await _restaurantOnboardingService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
