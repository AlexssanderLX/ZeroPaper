using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Repositories.Interfaces;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/public/tables")]
public class PublicOrderingController : ControllerBase
{
    private readonly IWorkspaceService _workspaceService;
    private readonly ICouponService _couponService;
    private readonly ISalesAgentService _salesAgentService;
    private readonly ISalesAgentRepository _salesAgentRepository;

    public PublicOrderingController(
        IWorkspaceService workspaceService,
        ICouponService couponService,
        ISalesAgentService salesAgentService,
        ISalesAgentRepository salesAgentRepository)
    {
        _workspaceService = workspaceService;
        _couponService = couponService;
        _salesAgentService = salesAgentService;
        _salesAgentRepository = salesAgentRepository;
    }

    [HttpGet("{publicCode}")]
    [ProducesResponseType(typeof(PublicTableViewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTableAsync(string publicCode, CancellationToken cancellationToken)
    {
        return Ok(await _workspaceService.GetPublicTableAsync(publicCode, cancellationToken));
    }

    [HttpGet("{publicCode}/menu/items/{menuItemId:guid}")]
    [ProducesResponseType(typeof(MenuItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenuItemAsync(string publicCode, Guid menuItemId, CancellationToken cancellationToken)
    {
        return Ok(await _workspaceService.GetPublicMenuItemAsync(publicCode, menuItemId, cancellationToken));
    }

    [HttpPost("{publicCode}/coupons/validate")]
    [EnableRateLimiting("public-write")]
    [ProducesResponseType(typeof(CouponValidationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateCouponAsync(
        string publicCode,
        [FromBody] ValidateCouponRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _couponService.ValidatePublicCouponAsync(publicCode, request, cancellationToken));
    }

    [HttpPost("{publicCode}/orders")]
    [EnableRateLimiting("public-write")]
    [ProducesResponseType(typeof(CustomerOrderDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOrderAsync(string publicCode, [FromBody] CreateCustomerOrderRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _workspaceService.CreatePublicOrderAsync(publicCode, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (InvalidOperationException exception) when (IsOutOfServiceWindow(exception))
        {
            return StatusCode(StatusCodes.Status409Conflict, new { message = exception.Message });
        }
    }

    [HttpPost("{publicCode}/freight/quote")]
    [EnableRateLimiting("public-write")]
    [ProducesResponseType(typeof(DeliveryFreightQuoteDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> QuoteDeliveryFreightAsync(
        string publicCode,
        [FromBody] DeliveryFreightQuoteRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _workspaceService.QuotePublicDeliveryFreightAsync(publicCode, request, cancellationToken));
        }
        catch (InvalidOperationException exception) when (IsOutOfServiceWindow(exception))
        {
            return StatusCode(StatusCodes.Status409Conflict, new { message = exception.Message });
        }
    }

    [HttpGet("{publicCode}/delivery/customer")]
    [ProducesResponseType(typeof(PublicDeliveryCustomerProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeliveryCustomerProfileAsync(
        string publicCode,
        [FromQuery] string? token,
        CancellationToken cancellationToken)
    {
        return Ok(await _workspaceService.GetPublicDeliveryCustomerProfileAsync(publicCode, token, cancellationToken));
    }

    [HttpGet("~/api/public/customer-profile/{code}")]
    [ProducesResponseType(typeof(PublicCustomerProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomerProfileAsync(string code, CancellationToken cancellationToken)
    {
        var response = await _workspaceService.GetPublicCustomerProfileAsync(code, cancellationToken);
        return response.Found ? Ok(response) : NotFound(response);
    }

    [HttpGet("~/api/public/delivery-links/{code}")]
    [ProducesResponseType(typeof(PublicDeliveryShortLinkDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResolveDeliveryShortLinkAsync(string code, CancellationToken cancellationToken)
    {
        var response = await _workspaceService.ResolvePublicDeliveryShortLinkAsync(code, cancellationToken);
        return response.Found ? Ok(response) : NotFound(response);
    }

    [HttpGet("~/api/public/delivery-links/{code}/tracking")]
    [ProducesResponseType(typeof(PublicOrderTrackingDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeliveryTrackingAsync(string code, CancellationToken cancellationToken)
    {
        var response = await _workspaceService.GetPublicDeliveryTrackingAsync(code, cancellationToken);
        return response.Found ? Ok(response) : NotFound(response);
    }

    [HttpGet("~/api/public/edit-links/{editCode}")]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public IActionResult ResolveEditShortLink(string editCode)
    {
        return StatusCode(StatusCodes.Status410Gone, BuildPublicEditDisabledResponse());
    }

    [HttpGet("{publicCode}/orders/edit/{editCode}")]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public IActionResult GetEditableOrder(string publicCode, string editCode)
    {
        return StatusCode(StatusCodes.Status410Gone, BuildPublicEditDisabledResponse());
    }

    [HttpPut("{publicCode}/orders/edit/{editCode}")]
    [EnableRateLimiting("public-write")]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public IActionResult UpdateOrder(string publicCode, string editCode, [FromBody] CreateCustomerOrderRequestDto request)
    {
        return StatusCode(StatusCodes.Status410Gone, BuildPublicEditDisabledResponse());
    }

    [HttpPost("{publicCode}/waiter-calls")]
    [EnableRateLimiting("public-write")]
    [ProducesResponseType(typeof(WaiterCallDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateWaiterCallAsync(string publicCode, CancellationToken cancellationToken)
    {
        var response = await _workspaceService.CreatePublicWaiterCallAsync(publicCode, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("~/api/public/seller-link/{code}")]
    [ProducesResponseType(typeof(PublicSellerLinkDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSellerLinkAsync(string code, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _salesAgentService.GetPublicSellerLinkAsync(code, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("~/api/public/seller-link/{code}/orders")]
    [EnableRateLimiting("public-write")]
    [ProducesResponseType(typeof(CustomerOrderDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSellerLinkOrderAsync(
        string code,
        [FromBody] CreateCustomerOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        var agent = await _salesAgentRepository.GetByCodeAsync(code.Trim().ToLowerInvariant(), cancellationToken);
        if (agent is null) return NotFound();

        try
        {
            var result = await _workspaceService.CreateSellerLinkOrderAsync(
                agent.Id, agent.TenantId, agent.CompanyId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (InvalidOperationException exception) when (IsOutOfServiceWindow(exception))
        {
            return StatusCode(StatusCodes.Status409Conflict, new { message = exception.Message });
        }
    }

    private static object BuildPublicEditDisabledResponse()
    {
        return new
        {
            message = "A edicao publica pos-pedido foi desativada. O pedido confirmado ja entra direto na operacao da unidade."
        };
    }

    private static bool IsOutOfServiceWindow(InvalidOperationException exception)
    {
        return exception.Message.Contains("fora do horario", StringComparison.OrdinalIgnoreCase) ||
               exception.Message.Contains("sistema de pedidos fica fechado", StringComparison.OrdinalIgnoreCase);
    }
}
