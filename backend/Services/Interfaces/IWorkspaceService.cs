using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Models;
using Microsoft.AspNetCore.Http;

namespace ZeroPaper.Services.Interfaces;

public interface IWorkspaceService
{
    Task<WorkspaceOverviewDto> GetOverviewAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MenuCategoryDto>> GetMenuAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<MenuCategoryDto> CreateMenuCategoryAsync(WorkspaceSessionContext session, CreateMenuCategoryRequestDto request, CancellationToken cancellationToken = default);
    Task<MenuItemDto> CreateMenuItemAsync(WorkspaceSessionContext session, CreateMenuItemRequestDto request, CancellationToken cancellationToken = default);
    Task<MenuItemDto> UpdateMenuItemStatusAsync(WorkspaceSessionContext session, Guid menuItemId, UpdateMenuItemStatusRequestDto request, CancellationToken cancellationToken = default);
    Task<UploadMenuItemImageResponseDto> UploadMenuItemImageAsync(WorkspaceSessionContext session, IFormFile file, CancellationToken cancellationToken = default);
    Task DeleteMenuCategoryAsync(WorkspaceSessionContext session, Guid categoryId, CancellationToken cancellationToken = default);
    Task DeleteMenuItemAsync(WorkspaceSessionContext session, Guid menuItemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiningTableDto>> GetTablesAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<DiningTableDto> CreateTableAsync(WorkspaceSessionContext session, CreateDiningTableRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerOrderDto>> GetOrdersAsync(WorkspaceSessionContext session, bool kitchenOnly, CancellationToken cancellationToken = default);
    Task<CustomerOrderDto> CreateOrderAsync(WorkspaceSessionContext session, CreateCustomerOrderRequestDto request, CancellationToken cancellationToken = default);
    Task<CustomerOrderDto> UpdateOrderStatusAsync(WorkspaceSessionContext session, Guid orderId, UpdateOrderStatusRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockItemDto>> GetStockItemsAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<StockItemDto> CreateStockItemAsync(WorkspaceSessionContext session, SaveStockItemRequestDto request, CancellationToken cancellationToken = default);
    Task<StockItemDto> UpdateStockItemAsync(WorkspaceSessionContext session, Guid stockItemId, SaveStockItemRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamMemberDto>> GetTeamMembersAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<TeamMemberDto> CreateTeamMemberAsync(WorkspaceSessionContext session, CreateTeamMemberRequestDto request, CancellationToken cancellationToken = default);
    Task<CompanySettingsDto> GetCompanySettingsAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<CompanySettingsDto> UpdateCompanySettingsAsync(WorkspaceSessionContext session, UpdateCompanySettingsRequestDto request, CancellationToken cancellationToken = default);
    Task<PublicTableViewDto> GetPublicTableAsync(string publicCode, CancellationToken cancellationToken = default);
    Task<CustomerOrderDto> CreatePublicOrderAsync(string publicCode, CreateCustomerOrderRequestDto request, CancellationToken cancellationToken = default);
}
