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
    private readonly ILogger<RestaurantOnboardingController> _logger;

    public RestaurantOnboardingController(
        IRestaurantOnboardingService restaurantOnboardingService,
        ILogger<RestaurantOnboardingController> logger)
    {
        _restaurantOnboardingService = restaurantOnboardingService;
        _logger = logger;
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
            _logger.LogWarning(exception, "Public signup payment is unavailable.");
            return Conflict(new ProblemDetails
            {
                Title = "Pagamento indisponivel",
                Detail = "O pagamento automatico esta temporariamente indisponivel. Tente novamente mais tarde ou use a opcao Solicitar acesso.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }
}
