using Microsoft.AspNetCore.Mvc;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/workspace")]
public class WorkspaceController : ControllerBase
{
    private readonly IAuthSessionService _authSessionService;
    private readonly IWorkspaceService _workspaceService;

    public WorkspaceController(IAuthSessionService authSessionService, IWorkspaceService workspaceService)
    {
        _authSessionService = authSessionService;
        _workspaceService = workspaceService;
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(WorkspaceOverviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.GetOverviewAsync(session, cancellationToken));
    }

    [HttpGet("menu")]
    [ProducesResponseType(typeof(IReadOnlyList<MenuCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenuAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.GetMenuAsync(session, cancellationToken));
    }

    [HttpPost("menu/categories")]
    [ProducesResponseType(typeof(MenuCategoryDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateMenuCategoryAsync([FromBody] CreateMenuCategoryRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        var response = await _workspaceService.CreateMenuCategoryAsync(session, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("menu/items")]
    [ProducesResponseType(typeof(MenuItemDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateMenuItemAsync([FromBody] CreateMenuItemRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        var response = await _workspaceService.CreateMenuItemAsync(session, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPatch("menu/items/{menuItemId:guid}/status")]
    [ProducesResponseType(typeof(MenuItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMenuItemStatusAsync(Guid menuItemId, [FromBody] UpdateMenuItemStatusRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.UpdateMenuItemStatusAsync(session, menuItemId, request, cancellationToken));
    }

    [HttpPost("menu/images")]
    [ProducesResponseType(typeof(UploadMenuItemImageResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadMenuItemImageAsync([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        var response = await _workspaceService.UploadMenuItemImageAsync(session, file, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("menu/categories/{categoryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMenuCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        await _workspaceService.DeleteMenuCategoryAsync(session, categoryId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("menu/items/{menuItemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMenuItemAsync(Guid menuItemId, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        await _workspaceService.DeleteMenuItemAsync(session, menuItemId, cancellationToken);
        return NoContent();
    }

    [HttpGet("tables")]
    [ProducesResponseType(typeof(IReadOnlyList<DiningTableDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTablesAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.GetTablesAsync(session, cancellationToken));
    }

    [HttpPost("tables")]
    [ProducesResponseType(typeof(DiningTableDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTableAsync([FromBody] CreateDiningTableRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        var response = await _workspaceService.CreateTableAsync(session, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("orders")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrdersAsync([FromQuery] bool kitchenOnly, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.GetOrdersAsync(session, kitchenOnly, cancellationToken));
    }

    [HttpPost("orders")]
    [ProducesResponseType(typeof(CustomerOrderDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOrderAsync([FromBody] CreateCustomerOrderRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        var response = await _workspaceService.CreateOrderAsync(session, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPatch("orders/{orderId:guid}/status")]
    [ProducesResponseType(typeof(CustomerOrderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateOrderStatusAsync(Guid orderId, [FromBody] UpdateOrderStatusRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.UpdateOrderStatusAsync(session, orderId, request, cancellationToken));
    }

    [HttpDelete("orders/{orderId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        await _workspaceService.DeleteOrderAsync(session, orderId, cancellationToken);
        return NoContent();
    }

    [HttpGet("stock")]
    [ProducesResponseType(typeof(IReadOnlyList<StockItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.GetStockItemsAsync(session, cancellationToken));
    }

    [HttpPost("stock")]
    [ProducesResponseType(typeof(StockItemDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateStockItemAsync([FromBody] SaveStockItemRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        var response = await _workspaceService.CreateStockItemAsync(session, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("stock/{stockItemId:guid}")]
    [ProducesResponseType(typeof(StockItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStockItemAsync(Guid stockItemId, [FromBody] SaveStockItemRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.UpdateStockItemAsync(session, stockItemId, request, cancellationToken));
    }

    [HttpGet("team")]
    [ProducesResponseType(typeof(IReadOnlyList<TeamMemberDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTeamAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.GetTeamMembersAsync(session, cancellationToken));
    }

    [HttpPost("team")]
    [ProducesResponseType(typeof(TeamMemberDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTeamMemberAsync([FromBody] CreateTeamMemberRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        var response = await _workspaceService.CreateTeamMemberAsync(session, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("settings")]
    [ProducesResponseType(typeof(CompanySettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.GetCompanySettingsAsync(session, cancellationToken));
    }

    [HttpPut("settings")]
    [ProducesResponseType(typeof(CompanySettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSettingsAsync([FromBody] UpdateCompanySettingsRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.UpdateCompanySettingsAsync(session, request, cancellationToken));
    }

    private async Task<WorkspaceSessionContext?> GetRequiredSessionAsync(CancellationToken cancellationToken)
    {
        return await _authSessionService.GetSessionAsync(Request.Headers.Authorization.ToString(), cancellationToken);
    }
}
