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

    [HttpPut("menu/categories/{categoryId:guid}")]
    [ProducesResponseType(typeof(MenuCategoryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMenuCategoryAsync(Guid categoryId, [FromBody] UpdateMenuCategoryRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.UpdateMenuCategoryAsync(session, categoryId, request, cancellationToken));
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

    [HttpPut("menu/items/{menuItemId:guid}")]
    [ProducesResponseType(typeof(MenuItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMenuItemAsync(Guid menuItemId, [FromBody] UpdateMenuItemRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.UpdateMenuItemAsync(session, menuItemId, request, cancellationToken));
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

    [HttpPut("tables/{tableId:guid}")]
    [ProducesResponseType(typeof(DiningTableDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTableAsync(Guid tableId, [FromBody] UpdateDiningTableRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.UpdateTableAsync(session, tableId, request, cancellationToken));
    }

    [HttpPost("tables/{tableId:guid}/alert-sound")]
    [ProducesResponseType(typeof(UploadTableAlertSoundResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadTableAlertSoundAsync(Guid tableId, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        return Ok(await _workspaceService.UploadTableAlertSoundAsync(session, tableId, file, cancellationToken));
    }

    [HttpDelete("tables/{tableId:guid}/alert-sound")]
    [ProducesResponseType(typeof(DiningTableDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetTableAlertSoundAsync(Guid tableId, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.ResetTableAlertSoundAsync(session, tableId, cancellationToken));
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

    [HttpPost("orders/batch-status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateOrdersStatusBatchAsync([FromBody] BatchUpdateOrderStatusRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        await _workspaceService.UpdateOrdersStatusBatchAsync(session, request, cancellationToken);
        return NoContent();
    }

    [HttpPatch("orders/{orderId:guid}/payment")]
    [ProducesResponseType(typeof(CustomerOrderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateOrderPaymentAsync(Guid orderId, [FromBody] UpdateOrderPaymentRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.UpdateOrderPaymentAsync(session, orderId, request, cancellationToken));
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

    [HttpPost("orders/{orderId:guid}/delete-paid")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePaidOrderAsync(Guid orderId, [FromBody] DeletePaidOrderRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        await _workspaceService.DeletePaidOrderAsync(session, orderId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("orders/delete-paid-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAllPaidOrdersAsync([FromBody] DeletePaidOrderRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        await _workspaceService.DeleteAllPaidOrdersAsync(session, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("orders/delete-closed")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteClosedOrdersBatchAsync([FromBody] BatchDeleteClosedOrdersRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        await _workspaceService.DeleteClosedOrdersBatchAsync(session, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("orders/delete-today-flow")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTodayOrderFlowAsync([FromBody] OwnerPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        await _workspaceService.DeleteTodayOrderFlowAsync(session, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("orders/daily-report")]
    [Produces("application/pdf")]
    public async Task<IActionResult> DownloadDailyCashReportAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        var reportFile = await _workspaceService.GenerateDailyCashReportPdfAsync(session, cancellationToken);
        return File(reportFile.Content, reportFile.ContentType, reportFile.FileName);
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

    [HttpPatch("settings/alerts")]
    [ProducesResponseType(typeof(AlertSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAlertSettingsAsync([FromBody] UpdateAlertSettingsRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.UpdateAlertSettingsAsync(session, request, cancellationToken));
    }

    [HttpPost("settings/alerts/sound")]
    [ProducesResponseType(typeof(UploadAlertSoundResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadAlertSoundAsync([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        return Ok(await _workspaceService.UploadAlertSoundAsync(session, file, cancellationToken));
    }

    [HttpDelete("settings/alerts/sound")]
    [ProducesResponseType(typeof(AlertSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetAlertSoundAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.ResetAlertSoundAsync(session, cancellationToken));
    }

    [HttpGet("waiter-calls")]
    [ProducesResponseType(typeof(IReadOnlyList<WaiterCallDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWaiterCallsAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.GetWaiterCallsAsync(session, cancellationToken));
    }

    [HttpGet("alerts/signal")]
    [ProducesResponseType(typeof(WorkspaceAlertsSignalDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlertsSignalAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.GetAlertsSignalAsync(session, cancellationToken));
    }

    [HttpPatch("waiter-calls/{waiterCallId:guid}/resolve")]
    [ProducesResponseType(typeof(WaiterCallDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResolveWaiterCallAsync(Guid waiterCallId, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.ResolveWaiterCallAsync(session, waiterCallId, cancellationToken));
    }

    private async Task<WorkspaceSessionContext?> GetRequiredSessionAsync(CancellationToken cancellationToken)
    {
        return await _authSessionService.GetSessionAsync(Request.Headers.Authorization.ToString(), cancellationToken);
    }
}
