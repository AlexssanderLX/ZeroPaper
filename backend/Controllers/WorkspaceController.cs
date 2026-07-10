using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.DTOs.Workspace.Reports;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;
using ZeroPaper.Services.Reports;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/workspace")]
public class WorkspaceController : ControllerBase
{
    private readonly IAuthSessionService _authSessionService;
    private readonly IWorkspaceService _workspaceService;
    private readonly ISalesReportService _salesReportService;
    private readonly ICouponService _couponService;
    private readonly ICashClosingService _cashClosingService;

    public WorkspaceController(
        IAuthSessionService authSessionService,
        IWorkspaceService workspaceService,
        ISalesReportService salesReportService,
        ICouponService couponService,
        ICashClosingService cashClosingService)
    {
        _authSessionService = authSessionService;
        _workspaceService = workspaceService;
        _salesReportService = salesReportService;
        _couponService = couponService;
        _cashClosingService = cashClosingService;
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
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesMenuModule, "Cardapio");
        return accessResult ?? Ok(await _workspaceService.GetMenuAsync(session, cancellationToken));
    }

    [HttpGet("menu/categories")]
    [ProducesResponseType(typeof(IReadOnlyList<MenuCategorySummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenuCategoriesAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesMenuModule, "Cardapio");
        return accessResult ?? Ok(await _workspaceService.GetMenuCategorySummariesAsync(session, cancellationToken));
    }

    [HttpGet("menu/categories/{categoryId:guid}/items")]
    [ProducesResponseType(typeof(MenuCategoryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenuCategoryItemsAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesMenuModule, "Cardapio");
        return accessResult ?? Ok(await _workspaceService.GetMenuCategoryItemsAsync(session, categoryId, cancellationToken));
    }

    [HttpGet("menu/items/{menuItemId:guid}")]
    [ProducesResponseType(typeof(MenuItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenuItemAsync(Guid menuItemId, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesMenuModule, "Cardapio");
        return accessResult ?? Ok(await _workspaceService.GetMenuItemAsync(session, menuItemId, cancellationToken));
    }

    [HttpGet("menu/additionals")]
    [ProducesResponseType(typeof(IReadOnlyList<MenuAdditionalCatalogGroupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenuAdditionalsAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesMenuModule, "Cardapio");
        return accessResult ?? Ok(await _workspaceService.GetMenuAdditionalCatalogGroupsAsync(session, cancellationToken));
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

        var accessResult = EnsureModuleEnabled(session.IncludesMenuModule, "Cardapio");
        if (accessResult is not null)
        {
            return accessResult;
        }

        var response = await _workspaceService.CreateMenuCategoryAsync(session, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("menu/categories/{categoryId:guid}")]
    [ProducesResponseType(typeof(MenuCategoryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMenuCategoryAsync(Guid categoryId, [FromBody] UpdateMenuCategoryRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesMenuModule, "Cardapio");
        return accessResult ?? Ok(await _workspaceService.UpdateMenuCategoryAsync(session, categoryId, request, cancellationToken));
    }

    [HttpPost("menu/additionals")]
    [ProducesResponseType(typeof(MenuAdditionalCatalogGroupDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateMenuAdditionalCatalogGroupAsync([FromBody] SaveMenuAdditionalCatalogGroupRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesMenuModule, "Cardapio");
        if (accessResult is not null)
        {
            return accessResult;
        }

        var response = await _workspaceService.CreateMenuAdditionalCatalogGroupAsync(session, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("menu/additionals/{groupId:guid}")]
    [ProducesResponseType(typeof(MenuAdditionalCatalogGroupDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMenuAdditionalCatalogGroupAsync(Guid groupId, [FromBody] SaveMenuAdditionalCatalogGroupRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesMenuModule, "Cardapio");
        return accessResult ?? Ok(await _workspaceService.UpdateMenuAdditionalCatalogGroupAsync(session, groupId, request, cancellationToken));
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

        var accessResult = EnsureModuleEnabled(session.IncludesMenuModule, "Cardapio");
        if (accessResult is not null)
        {
            return accessResult;
        }

        var response = await _workspaceService.CreateMenuItemAsync(session, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("menu/items/{menuItemId:guid}")]
    [ProducesResponseType(typeof(MenuItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMenuItemAsync(Guid menuItemId, [FromBody] UpdateMenuItemRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesMenuModule, "Cardapio");
        return accessResult ?? Ok(await _workspaceService.UpdateMenuItemAsync(session, menuItemId, request, cancellationToken));
    }

    [HttpPatch("menu/items/{menuItemId:guid}/status")]
    [ProducesResponseType(typeof(MenuItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMenuItemStatusAsync(Guid menuItemId, [FromBody] UpdateMenuItemStatusRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesMenuModule, "Cardapio");
        return accessResult ?? Ok(await _workspaceService.UpdateMenuItemStatusAsync(session, menuItemId, request, cancellationToken));
    }

    [HttpPost("menu/images")]
    [EnableRateLimiting("upload-write")]
    [ProducesResponseType(typeof(UploadMenuItemImageResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadMenuItemImageAsync([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesMenuModule, "Cardapio");
        if (accessResult is not null)
        {
            return accessResult;
        }

        var response = await _workspaceService.UploadMenuItemImageAsync(session, file, cancellationToken);
        return Ok(response);
    }

    [HttpPost("menu/categories/images")]
    [EnableRateLimiting("upload-write")]
    [ProducesResponseType(typeof(UploadMenuItemImageResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadMenuCategoryImageAsync([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesMenuModule, "Cardapio");
        if (accessResult is not null)
        {
            return accessResult;
        }

        var response = await _workspaceService.UploadMenuCategoryImageAsync(session, file, cancellationToken);
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

        var accessResult = EnsureModuleEnabled(session.IncludesMenuModule, "Cardapio");
        if (accessResult is not null)
        {
            return accessResult;
        }

        await _workspaceService.DeleteMenuCategoryAsync(session, categoryId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("menu/additionals/{groupId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMenuAdditionalCatalogGroupAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesMenuModule, "Cardapio");
        if (accessResult is not null)
        {
            return accessResult;
        }

        await _workspaceService.DeleteMenuAdditionalCatalogGroupAsync(session, groupId, cancellationToken);
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

        var accessResult = EnsureModuleEnabled(session.IncludesMenuModule, "Cardapio");
        if (accessResult is not null)
        {
            return accessResult;
        }

        await _workspaceService.DeleteMenuItemAsync(session, menuItemId, cancellationToken);
        return NoContent();
    }

    [HttpGet("tables")]
    [ProducesResponseType(typeof(IReadOnlyList<DiningTableDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTablesAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesTablesModule, "Mesas");
        return accessResult ?? Ok(await _workspaceService.GetTablesAsync(session, cancellationToken));
    }

    [HttpPost("tables/cash-order")]
    [ProducesResponseType(typeof(DiningTableDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> EnsureCashOrderTableAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.EnsureCashOrderTableAsync(session, cancellationToken));
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

        var accessResult = EnsureModuleEnabled(session.IncludesTablesModule, "Mesas");
        if (accessResult is not null)
        {
            return accessResult;
        }

        var response = await _workspaceService.CreateTableAsync(session, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("tables/delivery")]
    [ProducesResponseType(typeof(DiningTableDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> EnsureDeliveryTableAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesDeliveryModule, "Delivery");
        return accessResult ?? Ok(await _workspaceService.EnsureDeliveryTableAsync(session, cancellationToken));
    }

    [HttpPut("tables/{tableId:guid}")]
    [ProducesResponseType(typeof(DiningTableDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTableAsync(Guid tableId, [FromBody] UpdateDiningTableRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesTablesModule, "Mesas");
        return accessResult ?? Ok(await _workspaceService.UpdateTableAsync(session, tableId, request, cancellationToken));
    }

    [HttpPost("tables/{tableId:guid}/alert-sound")]
    [EnableRateLimiting("upload-write")]
    [ProducesResponseType(typeof(UploadTableAlertSoundResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadTableAlertSoundAsync(Guid tableId, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesTablesModule, "Mesas");
        if (accessResult is not null)
        {
            return accessResult;
        }

        return Ok(await _workspaceService.UploadTableAlertSoundAsync(session, tableId, file, cancellationToken));
    }

    [HttpDelete("tables/{tableId:guid}/alert-sound")]
    [ProducesResponseType(typeof(DiningTableDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetTableAlertSoundAsync(Guid tableId, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesTablesModule, "Mesas");
        return accessResult ?? Ok(await _workspaceService.ResetTableAlertSoundAsync(session, tableId, cancellationToken));
    }

    [HttpGet("orders")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrdersAsync([FromQuery] bool kitchenOnly, [FromQuery] bool summaryOnly, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesKitchenModule || session.IncludesCashModule, "Pedidos");
        return accessResult ?? Ok(await _workspaceService.GetOrdersAsync(session, kitchenOnly, summaryOnly, cancellationToken));
    }

    [HttpGet("orders/{orderId:guid}")]
    [ProducesResponseType(typeof(CustomerOrderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesKitchenModule || session.IncludesCashModule, "Pedidos");
        return accessResult ?? Ok(await _workspaceService.GetOrderAsync(session, orderId, cancellationToken));
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

        var accessResult = EnsureModuleEnabled(
            session.IncludesKitchenModule || session.IncludesCashModule || session.IncludesTablesModule || session.IncludesDeliveryModule,
            "Pedidos");
        if (accessResult is not null)
        {
            return accessResult;
        }

        var response = await _workspaceService.CreateOrderAsync(session, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("orders/{orderId:guid}")]
    [ProducesResponseType(typeof(CustomerOrderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateOrderAsync(Guid orderId, [FromBody] UpdateCustomerOrderRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesKitchenModule, "Cozinha");
        if (accessResult is not null)
        {
            return accessResult;
        }

        try
        {
            return Ok(await _workspaceService.UpdateOrderAsync(session, orderId, request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails { Detail = exception.Message });
        }
    }

    [HttpPatch("orders/{orderId:guid}/status")]
    [ProducesResponseType(typeof(CustomerOrderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateOrderStatusAsync(Guid orderId, [FromBody] UpdateOrderStatusRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesKitchenModule, "Cozinha");
        return accessResult ?? Ok(await _workspaceService.UpdateOrderStatusAsync(session, orderId, request, cancellationToken));
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

        var accessResult = EnsureModuleEnabled(session.IncludesKitchenModule, "Cozinha");
        if (accessResult is not null)
        {
            return accessResult;
        }

        await _workspaceService.UpdateOrdersStatusBatchAsync(session, request, cancellationToken);
        return NoContent();
    }

    [HttpPatch("orders/{orderId:guid}/payment")]
    [ProducesResponseType(typeof(CustomerOrderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateOrderPaymentAsync(Guid orderId, [FromBody] UpdateOrderPaymentRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesCashModule, "Caixa");
        return accessResult ?? Ok(await _workspaceService.UpdateOrderPaymentAsync(session, orderId, request, cancellationToken));
    }

    [HttpGet("customers/{phoneNumber}/profile")]
    [ProducesResponseType(typeof(CustomerProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomerProfileAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesDeliveryModule || session.IncludesCashModule, "Clientes");
        return accessResult ?? Ok(await _workspaceService.GetCustomerProfileAsync(session, phoneNumber, cancellationToken));
    }

    [HttpGet("customers/{phoneNumber}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerOrderHistoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomerOrderHistoryAsync(
        string phoneNumber,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesDeliveryModule || session.IncludesCashModule, "Clientes");
        return accessResult ?? Ok(await _workspaceService.GetCustomerOrderHistoryAsync(session, phoneNumber, limit, cancellationToken));
    }

    [HttpPut("customers/{phoneNumber}/profile")]
    [ProducesResponseType(typeof(CustomerProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCustomerProfileAsync(
        string phoneNumber,
        [FromBody] UpdateCustomerProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesDeliveryModule || session.IncludesCashModule, "Clientes");
        return accessResult ?? Ok(await _workspaceService.UpdateCustomerProfileAsync(session, phoneNumber, request, cancellationToken));
    }

    [HttpPatch("orders/mark-all-paid")]
    [ProducesResponseType(typeof(MarkAllOrdersPaidResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllOrdersPaidAsync([FromBody] MarkAllOrdersPaidRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesCashModule, "Caixa");
        return accessResult ?? Ok(await _workspaceService.MarkAllOrdersPaidAsync(session, request, cancellationToken));
    }

    [HttpPatch("orders/{orderId:guid}/adjustment")]
    [ProducesResponseType(typeof(CustomerOrderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AdjustOrderValueAsync(Guid orderId, [FromBody] AdjustOrderValueRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesCashModule, "Caixa");
        if (accessResult is not null)
        {
            return accessResult;
        }

        try
        {
            return Ok(await _workspaceService.AdjustOrderValueAsync(session, orderId, request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails { Detail = exception.Message });
        }
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

        var accessResult = EnsureModuleEnabled(session.IncludesCashModule, "Caixa");
        if (accessResult is not null)
        {
            return accessResult;
        }

        await _workspaceService.DeleteOrderAsync(session, orderId, cancellationToken);
        return NoContent();
    }

    [HttpPost("orders/{orderId:guid}/delete-paid")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePaidOrderAsync(Guid orderId, [FromBody] DeletePaidOrderRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesCashModule, "Caixa");
        if (accessResult is not null)
        {
            return accessResult;
        }

        await _workspaceService.DeletePaidOrderAsync(session, orderId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("orders/delete-paid-all")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAllPaidOrdersAsync([FromBody] DeletePaidOrderRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesCashModule, "Caixa");
        if (accessResult is not null)
        {
            return accessResult;
        }

        await _workspaceService.DeleteAllPaidOrdersAsync(session, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("orders/delete-closed")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteClosedOrdersBatchAsync([FromBody] BatchDeleteClosedOrdersRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesKitchenModule, "Cozinha");
        if (accessResult is not null)
        {
            return accessResult;
        }

        await _workspaceService.DeleteClosedOrdersBatchAsync(session, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("orders/delete-today-flow")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTodayOrderFlowAsync([FromBody] OwnerPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesCashModule, "Caixa");
        if (accessResult is not null)
        {
            return accessResult;
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

        var accessResult = EnsureModuleEnabled(session.IncludesCashModule, "Caixa");
        if (accessResult is not null)
        {
            return accessResult;
        }

        var reportFile = await _workspaceService.GenerateDailyCashReportPdfAsync(session, cancellationToken);
        return File(reportFile.Content, reportFile.ContentType, reportFile.FileName);
    }

    [HttpGet("stock")]
    [ProducesResponseType(typeof(IReadOnlyList<StockItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesStockModule, "Estoque");
        return accessResult ?? Ok(await _workspaceService.GetStockItemsAsync(session, cancellationToken));
    }

    [HttpGet("coupons")]
    [ProducesResponseType(typeof(IReadOnlyList<CouponDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCouponsAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.HasCoupons, "Cupons");
        return accessResult ?? Ok(await _couponService.GetCouponsAsync(session, cancellationToken));
    }

    [HttpPost("coupons")]
    [ProducesResponseType(typeof(CouponDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCouponAsync([FromBody] SaveCouponRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.HasCoupons, "Cupons");
        if (accessResult is not null)
        {
            return accessResult;
        }

        var response = await _couponService.CreateCouponAsync(session, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("coupons/{couponId:guid}")]
    [ProducesResponseType(typeof(CouponDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCouponAsync(Guid couponId, [FromBody] SaveCouponRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.HasCoupons, "Cupons");
        return accessResult ?? Ok(await _couponService.UpdateCouponAsync(session, couponId, request, cancellationToken));
    }

    [HttpPatch("coupons/{couponId:guid}/status")]
    [ProducesResponseType(typeof(CouponDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCouponStatusAsync(Guid couponId, [FromBody] UpdateCouponStatusRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.HasCoupons, "Cupons");
        return accessResult ?? Ok(await _couponService.UpdateCouponStatusAsync(session, couponId, request, cancellationToken));
    }

    [HttpPost("coupons/validate")]
    [ProducesResponseType(typeof(CouponValidationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateCouponAsync([FromBody] ValidateCouponRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.HasCoupons, "Cupons");
        return accessResult ?? Ok(await _couponService.ValidateWorkspaceCouponAsync(session, request, cancellationToken));
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

        var accessResult = EnsureModuleEnabled(session.IncludesStockModule, "Estoque");
        if (accessResult is not null)
        {
            return accessResult;
        }

        var response = await _workspaceService.CreateStockItemAsync(session, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("stock/{stockItemId:guid}")]
    [ProducesResponseType(typeof(StockItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStockItemAsync(Guid stockItemId, [FromBody] SaveStockItemRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesStockModule, "Estoque");
        return accessResult ?? Ok(await _workspaceService.UpdateStockItemAsync(session, stockItemId, request, cancellationToken));
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

    [HttpPost("settings/logo")]
    [EnableRateLimiting("upload-write")]
    [ProducesResponseType(typeof(CompanySettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadCompanyLogoAsync([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        return Ok(await _workspaceService.UploadCompanyLogoAsync(session, file, cancellationToken));
    }

    [HttpDelete("settings/logo")]
    [ProducesResponseType(typeof(CompanySettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetCompanyLogoAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.ResetCompanyLogoAsync(session, cancellationToken));
    }

    [HttpGet("profile")]
    [ProducesResponseType(typeof(OwnerProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOwnerProfileAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _workspaceService.GetOwnerProfileAsync(session, cancellationToken));
    }

    [HttpPut("profile")]
    [ProducesResponseType(typeof(OwnerProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateOwnerProfileAsync([FromBody] UpdateOwnerProfileRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _workspaceService.UpdateOwnerProfileAsync(session, request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails { Detail = exception.Message });
        }
    }

    [HttpPut("profile/password")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangeOwnerPasswordAsync([FromBody] ChangeOwnerPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        try
        {
            await _workspaceService.ChangeOwnerPasswordAsync(session, request, cancellationToken);
            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
    }

    [HttpPost("settings/shortcut-access")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(typeof(GenerateOwnerShortcutAccessResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RotateOwnerShortcutAccessAsync(
        [FromBody] GenerateOwnerShortcutAccessRequestDto request,
        CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _workspaceService.RotateOwnerShortcutAccessAsync(session, request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails { Detail = exception.Message });
        }
    }

    [HttpDelete("settings/shortcut-access")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(typeof(OwnerShortcutAccessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RevokeOwnerShortcutAccessAsync(
        [FromBody] GenerateOwnerShortcutAccessRequestDto request,
        CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _workspaceService.RevokeOwnerShortcutAccessAsync(session, request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails { Detail = exception.Message });
        }
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
    [EnableRateLimiting("upload-write")]
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

    [HttpGet("delivery/freight")]
    [ProducesResponseType(typeof(DeliveryFreightSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeliveryFreightSettingsAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesDeliveryModule, "Entrega");
        return accessResult ?? Ok(await _workspaceService.GetDeliveryFreightSettingsAsync(session, cancellationToken));
    }

    [HttpPatch("delivery/freight")]
    [ProducesResponseType(typeof(DeliveryFreightSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateDeliveryFreightSettingsAsync(
        [FromBody] UpdateDeliveryFreightSettingsRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Informe os dados do frete." });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Informe a senha owner para salvar o frete." });
        }

        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesDeliveryModule, "Entrega");
        if (accessResult is not null)
        {
            return accessResult;
        }

        try
        {
            return Ok(await _workspaceService.UpdateDeliveryFreightSettingsAsync(session, request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("waiter-calls")]
    [ProducesResponseType(typeof(IReadOnlyList<WaiterCallDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWaiterCallsAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesWaiterCallModule, "Chamado");
        return accessResult ?? Ok(await _workspaceService.GetWaiterCallsAsync(session, cancellationToken));
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
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesWaiterCallModule, "Chamado");
        return accessResult ?? Ok(await _workspaceService.ResolveWaiterCallAsync(session, waiterCallId, cancellationToken));
    }

    [HttpGet("reports/sales/{date}")]
    [ProducesResponseType(typeof(DailySalesReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDailySalesReportAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        return Ok(await _salesReportService.GetDailySalesReportAsync(session, date, cancellationToken));
    }

    [HttpGet("cash-closing/{date}")]
    [ProducesResponseType(typeof(CashClosingReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCashClosingAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureModuleEnabled(session.IncludesCashModule, "Caixa");
        return accessResult ?? Ok(await _cashClosingService.GetCashClosingAsync(session, date, cancellationToken));
    }

    private async Task<WorkspaceSessionContext?> GetRequiredSessionAsync(CancellationToken cancellationToken)
    {
        return await _authSessionService.GetSessionAsync(Request.Headers.Authorization.ToString(), cancellationToken);
    }

    private ObjectResult? EnsureModuleEnabled(bool enabled, string moduleName)
    {
        return enabled
            ? null
            : StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Modulo indisponivel",
                Detail = $"O modulo {moduleName} nao faz parte do plano atual da unidade."
            });
    }
}
