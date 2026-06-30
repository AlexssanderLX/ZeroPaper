using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.Domain.Plans;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Documents;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class WorkspaceService : IWorkspaceService
{
    private sealed record EditedOrderContext(
        string? DeliveryPhone,
        string? DeliveryAddress,
        string? DeliveryNumber,
        string? DeliveryComplement,
        string? DeliveryPostalCode,
        decimal DeliveryFreightAmount,
        decimal? DeliveryDistanceKm,
        string? DeliveryFreightProvider,
        PaymentMethod PaymentMethod);

    private static readonly CultureInfo PtBrCulture = new("pt-BR");
    private const string FulfillmentTypeDelivery = "Delivery";
    private const string FulfillmentTypePickup = "Pickup";
    private const string FulfillmentTypeLocal = "Local";
    private const string PickupAddressMarker = "Retirada no local";
    private readonly PublicAppOptions _publicAppOptions;
    private readonly ZeroPaperDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly string _uploadsRootPath;
    private readonly IWhatsAppIntegrationService _whatsAppIntegrationService;
    private readonly IDeliveryFreightService _deliveryFreightService;
    private readonly IDeliveryCustomerLinkService _deliveryCustomerLinkService;
    private readonly ICashOrderTableService _cashOrderTableService;
    private readonly ICouponService _couponService;
    private readonly ILogger<WorkspaceService> _logger;

    public WorkspaceService(
        ZeroPaperDbContext context,
        IPasswordHasher passwordHasher,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IOptions<PublicAppOptions> publicAppOptions,
        IWhatsAppIntegrationService whatsAppIntegrationService,
        IDeliveryFreightService deliveryFreightService,
        IDeliveryCustomerLinkService deliveryCustomerLinkService,
        ICashOrderTableService cashOrderTableService,
        ICouponService couponService,
        ILogger<WorkspaceService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _uploadsRootPath = ResolveUploadsRootPath(environment, configuration);
        _publicAppOptions = publicAppOptions.Value;
        _whatsAppIntegrationService = whatsAppIntegrationService;
        _deliveryFreightService = deliveryFreightService;
        _deliveryCustomerLinkService = deliveryCustomerLinkService;
        _cashOrderTableService = cashOrderTableService;
        _couponService = couponService;
        _logger = logger;
    }

    private static string ResolveUploadsRootPath(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configuredPath = configuration["Storage:UploadsPath"]
            ?? Environment.GetEnvironmentVariable("ZEROPAPER_UPLOADS_PATH");

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            configuredPath = environment.IsDevelopment()
                ? Path.Combine(environment.ContentRootPath, "wwwroot", "uploads")
                : Path.Combine("/var/lib/zeropaper", "uploads");
        }

        var fullPath = Path.GetFullPath(configuredPath);
        Directory.CreateDirectory(fullPath);
        return fullPath;
    }

    public async Task<WorkspaceOverviewDto> GetOverviewAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        var activeTables = await _context.DiningTables
            .CountAsync(
                item => item.CompanyId == session.CompanyId &&
                        item.IsActive &&
                        !item.IsDeliveryChannel &&
                        item.Status != TableStatus.Inactive,
                cancellationToken);

        var openOrders = await _context.CustomerOrders
            .CountAsync(
                item => item.CompanyId == session.CompanyId &&
                        item.IsActive &&
                        item.Status != OrderStatus.Cancelled &&
                        !(item.Status == OrderStatus.Delivered && item.PaymentStatus == PaymentStatus.Paid),
                cancellationToken);

        var publishedMenuItems = await _context.MenuItems
            .CountAsync(item => item.CompanyId == session.CompanyId && item.IsActive, cancellationToken);

        var totalMenuItems = await _context.MenuItems
            .CountAsync(item => item.CompanyId == session.CompanyId, cancellationToken);

        var totalStockItems = await _context.StockItems
            .CountAsync(item => item.CompanyId == session.CompanyId && item.IsActive, cancellationToken);

        var lowStockItems = await _context.StockItems
            .CountAsync(
                item => item.CompanyId == session.CompanyId &&
                        item.IsActive &&
                        item.CurrentQuantity <= item.MinimumQuantity,
                cancellationToken);

        var pendingPayments = await _context.CustomerOrders
            .CountAsync(
                item => item.CompanyId == session.CompanyId &&
                        item.IsActive &&
                        item.Status != OrderStatus.Cancelled &&
                        item.PaymentStatus != PaymentStatus.Paid,
                cancellationToken);

        var pendingPrints = await _context.CustomerOrders
            .CountAsync(
                item => item.CompanyId == session.CompanyId &&
                        item.IsActive &&
                        item.Status != OrderStatus.Cancelled &&
                        (item.PrintStatus == PrintStatus.Pending || item.PrintStatus == PrintStatus.Processing),
                cancellationToken);

        var printedPrints = await _context.CustomerOrders
            .CountAsync(
                item => item.CompanyId == session.CompanyId &&
                        item.IsActive &&
                        item.PrintStatus == PrintStatus.Printed,
                cancellationToken);

        var failedPrints = await _context.CustomerOrders
            .CountAsync(
                item => item.CompanyId == session.CompanyId &&
                        item.IsActive &&
                        item.PrintStatus == PrintStatus.Failed,
                cancellationToken);

        return new WorkspaceOverviewDto
        {
            ActiveTables = activeTables,
            OpenOrders = openOrders,
            PublishedMenuItems = publishedMenuItems,
            TotalMenuItems = totalMenuItems,
            TotalStockItems = totalStockItems,
            LowStockItems = lowStockItems,
            PendingPayments = pendingPayments,
            PendingPrints = pendingPrints,
            PrintedPrints = printedPrints,
            FailedPrints = failedPrints,
            PlanName = session.PlanName,
            PlanTier = session.PlanTier,
            IncludesMenuModule = session.IncludesMenuModule,
            IncludesTablesModule = session.IncludesTablesModule,
            IncludesKitchenModule = session.IncludesKitchenModule,
            IncludesCashModule = session.IncludesCashModule,
            IncludesStockModule = session.IncludesStockModule,
            IncludesDeliveryModule = session.IncludesDeliveryModule,
            IncludesPrintingModule = session.IncludesPrintingModule,
            IncludesWaiterCallModule = session.IncludesWaiterCallModule,
            IncludesAiAssistantModule = session.IncludesAiAssistantModule,
            HasWhatsAppAI = session.HasWhatsAppAI,
            HasDelivery = session.HasDelivery,
            HasAutoPrint = session.HasAutoPrint,
            HasBasicReports = session.HasBasicReports,
            HasManagementDashboard = session.HasManagementDashboard,
            HasAdvancedReports = session.HasAdvancedReports,
            HasCoupons = session.HasCoupons,
            HasRecurringCustomers = session.HasRecurringCustomers,
            HasSalesAgents = session.HasSalesAgents
        };
    }

    public async Task<IReadOnlyList<MenuCategoryDto>> GetMenuAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        return await BuildMenuAsync(session.CompanyId, includeInactiveItems: true, cancellationToken);
    }

    public async Task<IReadOnlyList<MenuCategorySummaryDto>> GetMenuCategorySummariesAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        var categories = await _context.MenuCategories
            .AsNoTracking()
            .Where(item => item.CompanyId == session.CompanyId && item.IsActive)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.ImageUrl,
                item.DisplayOrder
            })
            .ToListAsync(cancellationToken);

        if (categories.Count == 0)
        {
            return [];
        }

        var categoryIds = categories.Select(item => item.Id).ToList();
        var menuItems = await _context.MenuItems
            .AsNoTracking()
            .Where(item => item.CompanyId == session.CompanyId && categoryIds.Contains(item.MenuCategoryId))
            .Select(item => new
            {
                item.Id,
                item.MenuCategoryId,
                item.ImageUrl,
                item.Price,
                item.IsActive
            })
            .ToListAsync(cancellationToken);

        var activeMenuItemIds = menuItems
            .Where(item => item.IsActive)
            .Select(item => item.Id)
            .ToList();

        var itemIdsWithAdditionals = activeMenuItemIds.Count == 0
            ? new List<Guid>()
            : await _context.MenuItemAdditionalGroups
                .AsNoTracking()
                .Where(group =>
                    activeMenuItemIds.Contains(group.MenuItemId) &&
                    group.IsActive &&
                    group.MaxAdditionalSelections != 0 &&
                    group.Options.Any(option => option.IsActive))
                .Select(group => group.MenuItemId)
                .Distinct()
                .ToListAsync(cancellationToken);

        var itemIdWithAdditionalsLookup = itemIdsWithAdditionals.ToHashSet();
        var startingPriceLookup = await BuildMenuItemStartingPriceLookupAsync(activeMenuItemIds, cancellationToken);

        return categories.Select(category =>
        {
            var categoryItems = menuItems
                .Where(item => item.MenuCategoryId == category.Id)
                .ToList();

            var activeItems = categoryItems.Where(item => item.IsActive).ToList();

            return new MenuCategorySummaryDto
            {
                Id = category.Id,
                Name = category.Name,
                ImageUrl = NormalizeMenuImagePath(category.ImageUrl),
                DisplayOrder = category.DisplayOrder,
                TotalItems = categoryItems.Count,
                ActiveItems = activeItems.Count,
                HiddenItems = categoryItems.Count(item => !item.IsActive),
                ItemsWithoutImage = categoryItems.Count(item =>
                    string.IsNullOrWhiteSpace(item.ImageUrl) &&
                    string.IsNullOrWhiteSpace(category.ImageUrl)),
                ItemsWithAdditionals = categoryItems.Count(item => itemIdWithAdditionalsLookup.Contains(item.Id)),
                StartingPrice = activeItems
                    .Select(item => startingPriceLookup.GetValueOrDefault(item.Id, item.Price))
                    .Cast<decimal?>()
                    .DefaultIfEmpty()
                    .Min()
            };
        }).ToList();
    }

    public async Task<MenuCategoryDto> GetMenuCategoryItemsAsync(WorkspaceSessionContext session, Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = await _context.MenuCategories
            .AsNoTracking()
            .Where(item =>
                item.Id == categoryId &&
                item.CompanyId == session.CompanyId &&
                item.IsActive)
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.ImageUrl,
                item.DisplayOrder
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Categoria nao encontrada.");

        var items = await _context.MenuItems
            .AsNoTracking()
            .Where(item =>
                item.CompanyId == session.CompanyId &&
                item.MenuCategoryId == category.Id)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

        var menuItemIds = items.Select(item => item.Id).ToList();
        var itemIdsWithAdditionals = menuItemIds.Count == 0
            ? new List<Guid>()
            : await _context.MenuItemAdditionalGroups
                .AsNoTracking()
                .Where(group =>
                    menuItemIds.Contains(group.MenuItemId) &&
                    group.IsActive &&
                    group.MaxAdditionalSelections != 0 &&
                    group.Options.Any(option => option.IsActive))
                .Select(group => group.MenuItemId)
                .Distinct()
                .ToListAsync(cancellationToken);

        var itemIdWithAdditionalsLookup = itemIdsWithAdditionals.ToHashSet();
        var startingPriceLookup = await BuildMenuItemStartingPriceLookupAsync(menuItemIds, cancellationToken);

        return new MenuCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            ImageUrl = NormalizeMenuImagePath(category.ImageUrl),
            DisplayOrder = category.DisplayOrder,
            Items = items
                .Select(item => MapPublicMenuItem(
                    item,
                    itemIdWithAdditionalsLookup.Contains(item.Id),
                    startingPriceLookup.GetValueOrDefault(item.Id, item.Price)))
                .ToList()
        };
    }

    public async Task<MenuItemDto> GetMenuItemAsync(WorkspaceSessionContext session, Guid menuItemId, CancellationToken cancellationToken = default)
    {
        var menuItem = await GetMenuItemEntityAsync(session.CompanyId, menuItemId, cancellationToken)
            ?? throw new KeyNotFoundException("Item nao encontrado.");

        return MapMenuItem(menuItem);
    }

    public async Task<IReadOnlyList<MenuAdditionalCatalogGroupDto>> GetMenuAdditionalCatalogGroupsAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        var groups = await _context.MenuAdditionalCatalogGroups
            .AsNoTracking()
            .Include(item => item.Options)
            .Where(item => item.CompanyId == session.CompanyId && item.IsActive)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

        var groupIds = groups.Select(item => item.Id).ToList();
        var linkedItemRows = await _context.MenuItemAdditionalGroups
            .AsNoTracking()
            .Where(group =>
                group.CompanyId == session.CompanyId &&
                group.IsActive &&
                group.SourceMenuAdditionalCatalogGroupId.HasValue &&
                groupIds.Contains(group.SourceMenuAdditionalCatalogGroupId.Value) &&
                group.MenuItem.IsActive)
            .Select(group => new
            {
                GroupId = group.SourceMenuAdditionalCatalogGroupId!.Value,
                ItemName = group.MenuItem.Name
            })
            .ToListAsync(cancellationToken);

        var linkedItemsLookup = linkedItemRows
            .GroupBy(item => item.GroupId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(item => item.ItemName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(item => item)
                    .ToList());

        return groups.Select(group =>
        {
            var dto = MapMenuAdditionalCatalogGroup(group);
            var itemNames = linkedItemsLookup.GetValueOrDefault(group.Id) ?? new List<string>();
            dto.LinkedItemCount = itemNames.Count;
            dto.LinkedItemNames = itemNames.Take(4).ToList();
            return dto;
        }).ToList();
    }

    public async Task<MenuCategoryDto> CreateMenuCategoryAsync(WorkspaceSessionContext session, CreateMenuCategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        var nextDisplayOrder = await _context.MenuCategories
            .Where(item => item.CompanyId == session.CompanyId)
            .Select(item => (int?)item.DisplayOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var category = new MenuCategory(
            session.TenantId,
            session.CompanyId,
            request.Name,
            nextDisplayOrder + 1,
            NormalizeMenuImagePath(request.ImageUrl));

        await _context.MenuCategories.AddAsync(category, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new MenuCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            ImageUrl = NormalizeMenuImagePath(category.ImageUrl),
            DisplayOrder = category.DisplayOrder
        };
    }

    public async Task<MenuCategoryDto> UpdateMenuCategoryAsync(WorkspaceSessionContext session, Guid categoryId, UpdateMenuCategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        var category = await _context.MenuCategories
            .FirstOrDefaultAsync(
                item => item.Id == categoryId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Categoria nao encontrada.");

        category.Rename(request.Name);
        category.UpdateImage(NormalizeMenuImagePath(request.ImageUrl));
        await _context.SaveChangesAsync(cancellationToken);

        return new MenuCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            ImageUrl = NormalizeMenuImagePath(category.ImageUrl),
            DisplayOrder = category.DisplayOrder
        };
    }

    public async Task<MenuAdditionalCatalogGroupDto> CreateMenuAdditionalCatalogGroupAsync(WorkspaceSessionContext session, SaveMenuAdditionalCatalogGroupRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        var nextDisplayOrder = await _context.MenuAdditionalCatalogGroups
            .Where(item => item.CompanyId == session.CompanyId && item.IsActive)
            .Select(item => (int?)item.DisplayOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var group = new MenuAdditionalCatalogGroup(
            session.TenantId,
            session.CompanyId,
            request.Name,
            request.AllowMultiple,
            nextDisplayOrder + 1,
            NormalizeMaxAdditionalSelections(request.MaxAdditionalSelections));

        group.ReplaceOptions(BuildMenuAdditionalCatalogOptions(session, group.Id, request.Options));

        await _context.MenuAdditionalCatalogGroups.AddAsync(group, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapMenuAdditionalCatalogGroup(group);
    }

    public async Task<MenuAdditionalCatalogGroupDto> UpdateMenuAdditionalCatalogGroupAsync(WorkspaceSessionContext session, Guid groupId, SaveMenuAdditionalCatalogGroupRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var group = await _context.MenuAdditionalCatalogGroups
            .FirstOrDefaultAsync(
                item => item.Id == groupId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Categoria de complemento nao encontrada.");

        group.Rename(request.Name);
        group.SetAllowMultiple(request.AllowMultiple);
        group.SetMaxAdditionalSelections(NormalizeMaxAdditionalSelections(request.MaxAdditionalSelections));
        var replacementOptions = BuildMenuAdditionalCatalogOptions(session, group.Id, request.Options);

        await _context.SaveChangesAsync(cancellationToken);
        _context.ChangeTracker.Clear();

        await _context.MenuAdditionalCatalogOptions
            .Where(item => item.MenuAdditionalCatalogGroupId == group.Id)
            .ExecuteDeleteAsync(cancellationToken);

        if (replacementOptions.Count > 0)
        {
            await _context.MenuAdditionalCatalogOptions.AddRangeAsync(replacementOptions, cancellationToken);
        }

        var refreshedGroup = await GetMenuAdditionalCatalogGroupEntityAsync(session.CompanyId, group.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Categoria de complemento nao encontrada.");

        await SyncLinkedMenuItemAdditionalGroupsAsync(session.CompanyId, refreshedGroup, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return MapMenuAdditionalCatalogGroup(refreshedGroup);
    }

    public async Task DeleteMenuAdditionalCatalogGroupAsync(WorkspaceSessionContext session, Guid groupId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var group = await _context.MenuAdditionalCatalogGroups
            .Include(item => item.Options)
            .FirstOrDefaultAsync(
                item => item.Id == groupId &&
                        item.CompanyId == session.CompanyId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Categoria de complemento nao encontrada.");

        var linkedMenuItemAdditionalGroupIds = await _context.MenuItemAdditionalGroups
            .Where(item =>
                item.CompanyId == session.CompanyId &&
                item.SourceMenuAdditionalCatalogGroupId == group.Id)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);

        if (linkedMenuItemAdditionalGroupIds.Count > 0)
        {
            await _context.MenuItemAdditionalOptions
                .Where(item => linkedMenuItemAdditionalGroupIds.Contains(item.MenuItemAdditionalGroupId))
                .ExecuteDeleteAsync(cancellationToken);

            await _context.MenuItemAdditionalGroups
                .Where(item => linkedMenuItemAdditionalGroupIds.Contains(item.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        _context.MenuAdditionalCatalogGroups.Remove(group);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<MenuItemDto> CreateMenuItemAsync(WorkspaceSessionContext session, CreateMenuItemRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        var category = await _context.MenuCategories
            .FirstOrDefaultAsync(
                item => item.Id == request.CategoryId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Categoria nao encontrada.");

        var nextDisplayOrder = await _context.MenuItems
            .Where(item => item.MenuCategoryId == category.Id)
            .Select(item => (int?)item.DisplayOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var normalizedImageUrl = NormalizeMenuImagePath(request.ImageUrl);

        var menuItem = new MenuItem(
            session.TenantId,
            session.CompanyId,
            category.Id,
            request.Name,
            request.Price,
            request.Description,
            request.AccentLabel,
            normalizedImageUrl,
            nextDisplayOrder + 1,
            NormalizeMaxAdditionalSelections(request.MaxAdditionalSelections));

        menuItem.ReplaceAdditionalGroups(await BuildAdditionalGroupsAsync(session, menuItem.Id, request.AdditionalGroups, cancellationToken));

        await _context.MenuItems.AddAsync(menuItem, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapMenuItem(menuItem);
    }

    public async Task<MenuItemDto> UpdateMenuItemAsync(WorkspaceSessionContext session, Guid menuItemId, UpdateMenuItemRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var category = await _context.MenuCategories
            .FirstOrDefaultAsync(
                item => item.Id == request.CategoryId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Categoria nao encontrada.");

        var menuItem = await _context.MenuItems
            .FirstOrDefaultAsync(
                item => item.Id == menuItemId &&
                        item.CompanyId == session.CompanyId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Item nao encontrado.");

        if (menuItem.MenuCategoryId != category.Id)
        {
            var nextDisplayOrder = await _context.MenuItems
                .Where(item => item.MenuCategoryId == category.Id && item.Id != menuItem.Id)
                .Select(item => (int?)item.DisplayOrder)
                .MaxAsync(cancellationToken) ?? -1;

            menuItem.ChangeCategory(category.Id);
            menuItem.SetDisplayOrder(nextDisplayOrder + 1);
        }

        var normalizedImageUrl = NormalizeMenuImagePath(request.ImageUrl);

        menuItem.UpdateCatalog(request.Name, request.Description, request.AccentLabel, normalizedImageUrl);
        menuItem.UpdatePrice(request.Price);
        menuItem.UpdateAdditionalLimit(NormalizeMaxAdditionalSelections(request.MaxAdditionalSelections));
        var replacementGroups = await BuildAdditionalGroupsAsync(session, menuItem.Id, request.AdditionalGroups, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        _context.ChangeTracker.Clear();

        await _context.MenuItemAdditionalOptions
            .Where(item => item.MenuItemId == menuItem.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await _context.MenuItemAdditionalGroups
            .Where(item => item.MenuItemId == menuItem.Id)
            .ExecuteDeleteAsync(cancellationToken);

        if (replacementGroups.Count > 0)
        {
            await _context.MenuItemAdditionalGroups.AddRangeAsync(replacementGroups, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var refreshedMenuItem = await GetMenuItemEntityAsync(session.CompanyId, menuItem.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Item nao encontrado.");

        return MapMenuItem(refreshedMenuItem);
    }

    public async Task<MenuItemDto> UpdateMenuItemStatusAsync(WorkspaceSessionContext session, Guid menuItemId, UpdateMenuItemStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var menuItem = await _context.MenuItems
            .FirstOrDefaultAsync(
                item => item.Id == menuItemId &&
                        item.CompanyId == session.CompanyId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Item nao encontrado.");

        if (request.IsActive)
        {
            menuItem.Activate();
        }
        else
        {
            menuItem.Deactivate();
        }

        await _context.SaveChangesAsync(cancellationToken);
        var refreshedMenuItem = await GetMenuItemEntityAsync(session.CompanyId, menuItem.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Item nao encontrado.");

        return MapMenuItem(refreshedMenuItem);
    }

    public async Task<UploadMenuItemImageResponseDto> UploadMenuItemImageAsync(WorkspaceSessionContext session, IFormFile file, CancellationToken cancellationToken = default)
    {
        return await UploadMenuImageAsync(session, file, "menu", cancellationToken);
    }

    public async Task<UploadMenuItemImageResponseDto> UploadMenuCategoryImageAsync(WorkspaceSessionContext session, IFormFile file, CancellationToken cancellationToken = default)
    {
        return await UploadMenuImageAsync(session, file, "menu-categories", cancellationToken);
    }

    private async Task<UploadMenuItemImageResponseDto> UploadMenuImageAsync(
        WorkspaceSessionContext session,
        IFormFile file,
        string uploadArea,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length <= 0)
        {
            throw new ArgumentException("Selecione uma imagem valida.", nameof(file));
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            throw new ArgumentException("A imagem precisa ter no maximo 5 MB.", nameof(file));
        }

        var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        if (!allowedTypes.Contains(file.ContentType))
        {
            throw new ArgumentException("Use uma imagem JPG, PNG ou WEBP.", nameof(file));
        }

        var safeExtension = await SafeUploadValidator.GetImageExtensionAsync(file, cancellationToken);

        var relativeDirectory = Path.Combine(uploadArea, session.CompanyId.ToString("N"));
        var directoryPath = Path.Combine(_uploadsRootPath, relativeDirectory);
        Directory.CreateDirectory(directoryPath);

        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var filePath = Path.Combine(directoryPath, fileName);

        await using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var relativePath = "/" + Path.Combine("uploads", relativeDirectory, fileName).Replace("\\", "/");

        return new UploadMenuItemImageResponseDto
        {
            ImageUrl = relativePath
        };
    }

    public async Task DeleteMenuCategoryAsync(WorkspaceSessionContext session, Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = await _context.MenuCategories
            .Include(item => item.Items)
                .ThenInclude(item => item.AdditionalGroups)
                    .ThenInclude(item => item.Options)
            .FirstOrDefaultAsync(
                item => item.Id == categoryId &&
                        item.CompanyId == session.CompanyId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Categoria nao encontrada.");

        foreach (var item in category.Items.ToList())
        {
            _context.MenuItems.Remove(item);
        }

        _context.MenuCategories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteMenuItemAsync(WorkspaceSessionContext session, Guid menuItemId, CancellationToken cancellationToken = default)
    {
        var menuItem = await _context.MenuItems
            .FirstOrDefaultAsync(
                item => item.Id == menuItemId &&
                        item.CompanyId == session.CompanyId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Item nao encontrado.");

        _context.MenuItems.Remove(menuItem);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DiningTableDto>> GetTablesAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        var tables = await _context.DiningTables
            .AsNoTracking()
            .Include(item => item.QrCodeAccess)
            .Include(item => item.Orders)
            .Where(item => item.CompanyId == session.CompanyId && item.IsActive)
            .OrderBy(item => item.IsDeliveryChannel)
            .ThenBy(item => item.InternalCode)
            .ToListAsync(cancellationToken);

        return tables.Select(table => MapDiningTable(table)).ToList();
    }

    public async Task<DiningTableDto> EnsureCashOrderTableAsync(
        WorkspaceSessionContext session,
        CancellationToken cancellationToken = default)
    {
        var table = await _cashOrderTableService.EnsureAsync(session.TenantId, session.CompanyId, cancellationToken);
        return MapDiningTable(table);
    }

    public async Task<DiningTableDto> CreateTableAsync(WorkspaceSessionContext session, CreateDiningTableRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        var internalCode = await GenerateUniqueTableCodeAsync(session.CompanyId, cancellationToken);
        var publicCode = await GenerateUniquePublicCodeAsync(cancellationToken);
        var accessPath = $"/q/{publicCode}";

        var qrCodeAccess = new QrCodeAccess(
            session.TenantId,
            session.CompanyId,
            $"Mesa {request.Name}",
            accessPath,
            publicCode: publicCode);

        var table = new DiningTable(
            session.TenantId,
            session.CompanyId,
            qrCodeAccess.Id,
            request.Name,
            internalCode,
            request.Seats,
            request.ComandaLabel);

        await _context.QrCodeAccesses.AddAsync(qrCodeAccess, cancellationToken);
        await _context.DiningTables.AddAsync(table, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapDiningTable(table, qrCodeAccess.PublicCode, qrCodeAccess.AccessPath);
    }

    public async Task<DiningTableDto> EnsureDeliveryTableAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        var existingTable = await _context.DiningTables
            .Include(item => item.QrCodeAccess)
            .Include(item => item.Orders)
            .FirstOrDefaultAsync(
                item => item.CompanyId == session.CompanyId &&
                        item.IsActive &&
                        item.IsDeliveryChannel,
                cancellationToken);

        if (existingTable is not null)
        {
            return MapDiningTable(existingTable);
        }

        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");

        var internalCode = await GenerateUniqueTableCodeAsync(session.CompanyId, "DELIVERY", cancellationToken);
        var publicCode = await GenerateUniquePublicCodeAsync(cancellationToken);
        var accessPath = $"/q/{publicCode}";
        var deliveryLabel = string.IsNullOrWhiteSpace(company.TradeName)
            ? "Delivery"
            : $"Delivery {company.TradeName}";

        var qrCodeAccess = new QrCodeAccess(
            session.TenantId,
            session.CompanyId,
            deliveryLabel,
            accessPath,
            publicCode: publicCode);

        var deliveryTable = new DiningTable(
            session.TenantId,
            session.CompanyId,
            qrCodeAccess.Id,
            "Delivery",
            internalCode,
            1,
            null,
            isDeliveryChannel: true);

        await _context.QrCodeAccesses.AddAsync(qrCodeAccess, cancellationToken);
        await _context.DiningTables.AddAsync(deliveryTable, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapDiningTable(deliveryTable, qrCodeAccess.PublicCode, qrCodeAccess.AccessPath);
    }

    public async Task<DiningTableDto> UpdateTableAsync(WorkspaceSessionContext session, Guid tableId, UpdateDiningTableRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        var table = await _context.DiningTables
            .Include(item => item.QrCodeAccess)
            .FirstOrDefaultAsync(
                item => item.Id == tableId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Mesa nao encontrada.");

        table.Rename(request.Name);
        table.UpdateSeats(request.Seats);
        table.UpdateComandaLabel(request.ComandaLabel);

        await _context.SaveChangesAsync(cancellationToken);

        return MapDiningTable(
            table,
            table.QrCodeAccess.PublicCode,
            table.QrCodeAccess.AccessPath,
            await _context.CustomerOrders.CountAsync(
                order => order.DiningTableId == table.Id &&
                         order.IsActive &&
                         order.Status != OrderStatus.Delivered &&
                         order.Status != OrderStatus.Cancelled,
                cancellationToken));
    }

    public async Task<UploadTableAlertSoundResponseDto> UploadTableAlertSoundAsync(
        WorkspaceSessionContext session,
        Guid tableId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length <= 0)
        {
            throw new ArgumentException("Selecione um audio valido.", nameof(file));
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            throw new ArgumentException("O audio precisa ter no maximo 10 MB.", nameof(file));
        }

        var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "audio/wav",
            "audio/x-wav",
            "audio/mpeg",
            "audio/mp3",
            "audio/ogg"
        };

        if (!allowedTypes.Contains(file.ContentType))
        {
            throw new ArgumentException("Use um arquivo WAV, MP3 ou OGG.", nameof(file));
        }

        var table = await _context.DiningTables
            .Include(item => item.QrCodeAccess)
            .FirstOrDefaultAsync(
                item => item.Id == tableId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Mesa nao encontrada.");

        var safeExtension = await SafeUploadValidator.GetAudioExtensionAsync(file, cancellationToken);

        var relativeDirectory = Path.Combine("alerts", session.CompanyId.ToString("N"), "tables", table.Id.ToString("N"));
        var directoryPath = Path.Combine(_uploadsRootPath, relativeDirectory);
        Directory.CreateDirectory(directoryPath);

        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var filePath = Path.Combine(directoryPath, fileName);

        await using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var relativePath = "/" + Path.Combine("uploads", relativeDirectory, fileName).Replace("\\", "/");
        table.UpdateAlertSound(relativePath);
        await _context.SaveChangesAsync(cancellationToken);

        return new UploadTableAlertSoundResponseDto
        {
            Table = MapDiningTable(
                table,
                table.QrCodeAccess.PublicCode,
                table.QrCodeAccess.AccessPath,
                await _context.CustomerOrders.CountAsync(
                    order => order.DiningTableId == table.Id &&
                             order.IsActive &&
                             order.Status != OrderStatus.Delivered &&
                             order.Status != OrderStatus.Cancelled,
                    cancellationToken))
        };
    }

    public async Task<DiningTableDto> ResetTableAlertSoundAsync(
        WorkspaceSessionContext session,
        Guid tableId,
        CancellationToken cancellationToken = default)
    {
        var table = await _context.DiningTables
            .Include(item => item.QrCodeAccess)
            .FirstOrDefaultAsync(
                item => item.Id == tableId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Mesa nao encontrada.");

        table.UpdateAlertSound(null);
        await _context.SaveChangesAsync(cancellationToken);

        return MapDiningTable(
            table,
            table.QrCodeAccess.PublicCode,
            table.QrCodeAccess.AccessPath,
            await _context.CustomerOrders.CountAsync(
                order => order.DiningTableId == table.Id &&
                         order.IsActive &&
                         order.Status != OrderStatus.Delivered &&
                         order.Status != OrderStatus.Cancelled,
                cancellationToken));
    }

    public async Task<IReadOnlyList<CustomerOrderDto>> GetOrdersAsync(
        WorkspaceSessionContext session,
        bool kitchenOnly,
        bool summaryOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CustomerOrders
            .AsNoTracking()
            .Include(item => item.DiningTable)
                .ThenInclude(item => item.QrCodeAccess)
            .Include(item => item.Payments)
            .Where(item => item.CompanyId == session.CompanyId && item.IsActive);

        query = summaryOnly
            ? query.Include(item => item.Items)
            : query.Include(item => item.Items).ThenInclude(item => item.AdditionalSelections);

        if (kitchenOnly)
        {
            query = query.Where(item =>
                item.Status == OrderStatus.Pending ||
                item.Status == OrderStatus.InKitchen ||
                item.Status == OrderStatus.Ready);
        }

        var orders = await query
            .OrderByDescending(item => item.SubmittedAtUtc)
            .Take(80)
            .ToListAsync(cancellationToken);

        return orders.Select(order => MapOrder(order, includeItems: !summaryOnly)).ToList();
    }

    public async Task<CustomerOrderDto> GetOrderAsync(
        WorkspaceSessionContext session,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.CustomerOrders
            .AsNoTracking()
            .Include(item => item.Items)
                .ThenInclude(item => item.AdditionalSelections)
            .Include(item => item.DiningTable)
                .ThenInclude(item => item.QrCodeAccess)
            .Include(item => item.Payments)
            .FirstOrDefaultAsync(
                item => item.Id == orderId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Pedido nao encontrado.");

        return MapOrder(order);
    }

    public async Task<CustomerOrderDto> CreateOrderAsync(WorkspaceSessionContext session, CreateCustomerOrderRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.TableId.HasValue)
        {
            throw new ArgumentException("A valid table must be informed.", nameof(request.TableId));
        }

        var table = await _context.DiningTables
            .Include(item => item.QrCodeAccess)
            .FirstOrDefaultAsync(
                item => item.Id == request.TableId.Value &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Table not found.");

        if (!string.IsNullOrWhiteSpace(request.CouponCode) && !session.HasCoupons)
        {
            throw new InvalidOperationException("Cupons nao fazem parte do plano atual da unidade.");
        }

        return await CreateOrderForTableAsync(
            table,
            request.CustomerName,
            request.Notes,
            request.DeliveryPhone,
            request.DeliveryAddress,
            request.DeliveryNumber,
            request.DeliveryNeighborhood,
            request.DeliveryComplement,
            request.DeliveryPostalCode,
            request.FulfillmentType,
            request.Items,
            request.MenuSelections,
            ParsePaymentMethodOrDefault(request.PaymentMethod),
            request.CouponCode,
            cancellationToken);
    }

    public async Task<CustomerOrderDto> UpdateOrderAsync(WorkspaceSessionContext session, Guid orderId, UpdateCustomerOrderRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                var order = await _context.CustomerOrders
                    .Include(item => item.Items)
                    .Include(item => item.DiningTable)
                        .ThenInclude(item => item.QrCodeAccess)
                    .Include(item => item.Payments)
                    .FirstOrDefaultAsync(
                        item => item.Id == orderId &&
                                item.CompanyId == session.CompanyId &&
                                item.IsActive,
                        cancellationToken)
                    ?? throw new KeyNotFoundException("Pedido nao encontrado.");

                EnsureOrderCanBeEdited(order);

                var updatedItems = request.MenuSelections.Count > 0
                    ? await BuildOrderItemsAsync(session.CompanyId, session.TenantId, [], request.MenuSelections, cancellationToken)
                    : BuildEditedOrderItems(session, order, request.Items);

                if (request.Items.Count > 0)
                {
                    updatedItems.AddRange(BuildEditedOrderItems(session, order, request.Items));
                }

                if (updatedItems.Count == 0)
                {
                    throw new ArgumentException("O pedido precisa manter pelo menos um item.");
                }

                var updateContext = await BuildEditedOrderContextAsync(order, request, updatedItems, cancellationToken);

                order.UpdateOrderDetails(
                    request.CustomerName,
                    request.Notes,
                    updateContext.DeliveryPhone,
                    updateContext.DeliveryAddress,
                    updateContext.DeliveryNumber,
                    updateContext.DeliveryComplement,
                    updateContext.DeliveryPostalCode,
                    updateContext.DeliveryFreightAmount,
                    updateContext.DeliveryDistanceKm,
                    updateContext.DeliveryFreightProvider,
                    updateContext.PaymentMethod);
                order.ApplyEditedItemsTotal(updatedItems.Sum(item => item.TotalPrice));
                await _couponService.ReapplyOrderCouponAsync(order, updatedItems.Sum(item => item.TotalPrice), cancellationToken);
                order.MarkEdited(DateTime.UtcNow);

                await _context.OrderItemAdditionalSelections
                    .Where(item => _context.OrderItems
                        .Where(orderItem => orderItem.CustomerOrderId == order.Id)
                        .Select(orderItem => orderItem.Id)
                        .Contains(item.OrderItemId))
                    .ExecuteDeleteAsync(cancellationToken);

                await _context.OrderItems
                    .Where(item => item.CustomerOrderId == order.Id)
                    .ExecuteDeleteAsync(cancellationToken);

                foreach (var item in updatedItems)
                {
                    item.AttachToCustomerOrder(order.Id);
                }

                await _context.OrderItems.AddRangeAsync(updatedItems, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                _context.ChangeTracker.Clear();
                return await GetOrderAsync(session, orderId, cancellationToken);
            }
            catch (DbUpdateConcurrencyException) when (attempt == 0)
            {
                _context.ChangeTracker.Clear();
            }
        }

        throw new DbUpdateConcurrencyException("Nao foi possivel salvar a edicao porque o pedido mudou durante a operacao.");
    }

    public async Task<CustomerOrderDto> AdjustOrderValueAsync(WorkspaceSessionContext session, Guid orderId, AdjustOrderValueRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var order = await _context.CustomerOrders
            .Include(item => item.Items)
                .ThenInclude(item => item.AdditionalSelections)
            .Include(item => item.DiningTable)
            .Include(item => item.Payments)
            .FirstOrDefaultAsync(
                item => item.Id == orderId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Pedido nao encontrado.");

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Pedido cancelado nao pode ter valor ajustado.");
        }

        if (request.FinalAmount.HasValue)
        {
            order.ApplyFinalAmountAdjustment(request.FinalAmount.Value, request.Note, DateTime.UtcNow);
        }
        else
        {
            order.ApplyPriceAdjustment(request.DiscountAmount, request.SurchargeAmount, request.Note, DateTime.UtcNow);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return MapOrder(order);
    }

    public async Task<CustomerOrderDto> UpdateOrderStatusAsync(WorkspaceSessionContext session, Guid orderId, UpdateOrderStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var order = await _context.CustomerOrders
            .Include(item => item.Items)
                .ThenInclude(item => item.AdditionalSelections)
            .Include(item => item.DiningTable)
            .Include(item => item.Payments)
            .FirstOrDefaultAsync(
                item => item.Id == orderId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");

        var nextStatus = ParseOrderStatus(request.Status);

        await ApplyOrderStatusAsync(order, nextStatus, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return MapOrder(order);
    }

    public async Task UpdateOrdersStatusBatchAsync(WorkspaceSessionContext session, BatchUpdateOrderStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var orderIds = request.OrderIds
            .Where(item => item != Guid.Empty)
            .Distinct()
            .ToList();

        if (orderIds.Count == 0)
        {
            throw new ArgumentException("Selecione pelo menos um pedido.", nameof(request.OrderIds));
        }

        var nextStatus = ParseOrderStatus(request.Status);

        var orders = await _context.CustomerOrders
            .Include(item => item.Items)
                .ThenInclude(item => item.AdditionalSelections)
            .Include(item => item.DiningTable)
            .Where(item =>
                item.CompanyId == session.CompanyId &&
                item.IsActive &&
                orderIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

        if (orders.Count == 0)
        {
            return;
        }

        if (nextStatus is OrderStatus.Delivered)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Password);
            await ValidateOwnerPasswordAsync(session, request.Password, cancellationToken);
        }

        foreach (var order in orders)
        {
            await ApplyOrderStatusAsync(order, nextStatus, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<CustomerOrderDto> UpdateOrderPaymentAsync(WorkspaceSessionContext session, Guid orderId, UpdateOrderPaymentRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var order = await _context.CustomerOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == orderId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Pedido nao encontrado.");

        var paymentStatus = ParsePaymentStatus(request.PaymentStatus);
        var paymentMethod = !string.IsNullOrWhiteSpace(request.PaymentMethod)
            ? ParsePaymentMethod(request.PaymentMethod)
            : order.PaymentMethod;
        var paidAtUtc = DateTime.UtcNow;
        var updatedAtUtc = DateTime.UtcNow;

        switch (paymentStatus)
        {
            case PaymentStatus.Pending:
                await _context.CustomerOrderPayments
                    .Where(item => item.CustomerOrderId == order.Id)
                    .ExecuteDeleteAsync(cancellationToken);

                var pendingRows = await _context.CustomerOrders
                    .Where(item => item.Id == order.Id && item.CompanyId == session.CompanyId && item.IsActive)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.PaymentStatus, PaymentStatus.Pending)
                        .SetProperty(item => item.PaymentMethod, paymentMethod)
                        .SetProperty(item => item.PaidAtUtc, (DateTime?)null)
                        .SetProperty(item => item.UpdatedAtUtc, updatedAtUtc),
                        cancellationToken);
                if (pendingRows == 0)
                {
                    throw new KeyNotFoundException("Pedido nao encontrado.");
                }
                break;
            case PaymentStatus.Paid:
                if (order.Status == OrderStatus.Cancelled)
                {
                    throw new InvalidOperationException("Pedido cancelado nao pode ser marcado como pago.");
                }

                List<CustomerOrderPayment> payments;
                if (request.Payments.Count > 0)
                {
                    payments = request.Payments
                        .Where(item => item.Amount > 0)
                        .Select(item => new CustomerOrderPayment(
                            session.TenantId,
                            order.Id,
                            ParsePaymentMethod(item.Method),
                            item.Amount))
                        .ToList();

                    if (payments.Count == 0)
                    {
                        throw new ArgumentException("Informe pelo menos um pagamento valido.", nameof(request.Payments));
                    }
                }
                else
                {
                    if (paymentMethod == PaymentMethod.Undefined)
                    {
                        throw new ArgumentException("Selecione a forma de pagamento no caixa.", nameof(request.PaymentMethod));
                    }

                    payments = [
                        new CustomerOrderPayment(session.TenantId, order.Id, paymentMethod, order.TotalAmount)
                    ];
                }

                var paymentTotal = decimal.Round(payments.Sum(item => item.Amount), 2);
                if (paymentTotal != order.TotalAmount)
                {
                    throw new InvalidOperationException("A soma dos pagamentos precisa bater com o total final do pedido.");
                }

                var finalPaymentMethod = payments.Count == 1 ? payments[0].Method : PaymentMethod.Undefined;

                await _context.CustomerOrderPayments
                    .Where(item => item.CustomerOrderId == order.Id)
                    .ExecuteDeleteAsync(cancellationToken);

                await _context.CustomerOrderPayments.AddRangeAsync(payments, cancellationToken);

                var paidRows = await _context.CustomerOrders
                    .Where(item => item.Id == order.Id && item.CompanyId == session.CompanyId && item.IsActive && item.Status != OrderStatus.Cancelled)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.PaymentStatus, PaymentStatus.Paid)
                        .SetProperty(item => item.PaymentMethod, finalPaymentMethod)
                        .SetProperty(item => item.PaidAtUtc, paidAtUtc)
                        .SetProperty(item => item.UpdatedAtUtc, updatedAtUtc),
                        cancellationToken);
                if (paidRows == 0)
                {
                    _context.ChangeTracker.Clear();
                    throw new InvalidOperationException("Pedido cancelado ou indisponivel para pagamento.");
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request.PaymentStatus), "Unsupported payment status.");
        }

        await _context.SaveChangesAsync(cancellationToken);

        return MapOrder(await _context.CustomerOrders
            .Include(item => item.Items)
                .ThenInclude(item => item.AdditionalSelections)
            .Include(item => item.DiningTable)
            .Include(item => item.Payments)
            .FirstAsync(item => item.Id == order.Id && item.CompanyId == session.CompanyId, cancellationToken));
    }

    public async Task<CustomerProfileDto> GetCustomerProfileAsync(
        WorkspaceSessionContext session,
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = NormalizeCustomerProfilePhone(phoneNumber);
        var profile = await _context.DeliveryCustomerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.CompanyId == session.CompanyId &&
                        item.Phone == normalizedPhone &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Cliente nao encontrado.");

        return MapCustomerProfile(profile);
    }

    public async Task<IReadOnlyList<CustomerOrderHistoryDto>> GetCustomerOrderHistoryAsync(
        WorkspaceSessionContext session,
        string phoneNumber,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = NormalizeCustomerProfilePhone(phoneNumber);
        var profileId = await _context.DeliveryCustomerProfiles
            .AsNoTracking()
            .Where(item => item.CompanyId == session.CompanyId &&
                           item.Phone == normalizedPhone &&
                           item.IsActive)
            .Select(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (profileId == Guid.Empty)
        {
            throw new KeyNotFoundException("Cliente nao encontrado.");
        }

        var take = Math.Clamp(limit.GetValueOrDefault(50), 1, 50);

        return await _context.CustomerOrderHistories
            .AsNoTracking()
            .Include(item => item.Items)
            .Where(item => item.CompanyId == session.CompanyId &&
                           item.CustomerProfileId == profileId &&
                           item.IsActive)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(take)
            .Select(item => new CustomerOrderHistoryDto
            {
                OrderId = item.OrderId,
                CreatedAtUtc = item.CreatedAtUtc,
                TotalAmount = item.TotalAmount,
                Items = item.Items
                    .OrderBy(historyItem => historyItem.CreatedAtUtc)
                    .Select(historyItem => new CustomerOrderHistoryItemDto
                    {
                        ItemName = historyItem.ItemName,
                        Quantity = historyItem.Quantity
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerProfileDto> UpdateCustomerProfileAsync(
        WorkspaceSessionContext session,
        string phoneNumber,
        UpdateCustomerProfileRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedPhone = NormalizeCustomerProfilePhone(phoneNumber);
        var profile = await _context.DeliveryCustomerProfiles
            .FirstOrDefaultAsync(
                item => item.CompanyId == session.CompanyId &&
                        item.Phone == normalizedPhone &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Cliente nao encontrado.");

        profile.UpdateOwnerData(
            NormalizeOptionalCustomerText(request.Name, 120),
            NormalizeOptionalCustomerText(request.Street, 220),
            NormalizeOptionalCustomerText(request.Number, 30),
            NormalizeOptionalCustomerText(request.Neighborhood, 120),
            NormalizeOptionalCustomerText(request.Complement, 160),
            NormalizeOptionalPostalCode(request.ZipCode));

        await _context.SaveChangesAsync(cancellationToken);
        return MapCustomerProfile(profile);
    }

    public async Task<MarkAllOrdersPaidResultDto> MarkAllOrdersPaidAsync(WorkspaceSessionContext session, MarkAllOrdersPaidRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestedOrders = request.Orders
            .Where(item => item.OrderId != Guid.Empty)
            .GroupBy(item => item.OrderId)
            .ToDictionary(item => item.Key, item => item.First());

        if (requestedOrders.Count == 0)
        {
            return new MarkAllOrdersPaidResultDto();
        }

        var orders = await _context.CustomerOrders
            .Include(item => item.Items)
                .ThenInclude(item => item.AdditionalSelections)
            .Include(item => item.DiningTable)
            .Where(item =>
                item.CompanyId == session.CompanyId &&
                item.IsActive &&
                requestedOrders.Keys.Contains(item.Id))
            .ToListAsync(cancellationToken);

        var result = new MarkAllOrdersPaidResultDto();
        var orderLookup = orders.ToDictionary(item => item.Id);
        var paymentPlans = new List<(CustomerOrder Order, List<CustomerOrderPayment> Payments)>();

        foreach (var requestOrder in requestedOrders.Values)
        {
            if (!orderLookup.TryGetValue(requestOrder.OrderId, out var order))
            {
                result.IgnoredCount++;
                result.IgnoredReasons.Add("Pedido nao encontrado para esta empresa.");
                continue;
            }

            if (order.PaymentStatus == PaymentStatus.Paid || order.Status == OrderStatus.Cancelled)
            {
                result.IgnoredCount++;
                result.IgnoredReasons.Add($"Pedido #{order.Number} ja estava pago ou cancelado.");
                continue;
            }

            try
            {
                var payments = (requestOrder.Payments ?? [])
                    .Where(item => item.Amount > 0)
                    .Select(item => new CustomerOrderPayment(session.TenantId, order.Id, ParsePaymentMethod(item.Method), item.Amount))
                    .ToList();

                if (payments.Count == 0)
                {
                    var method = string.IsNullOrWhiteSpace(requestOrder.PaymentMethod)
                        ? order.PaymentMethod
                        : ParsePaymentMethod(requestOrder.PaymentMethod);

                    if (method == PaymentMethod.Undefined)
                    {
                        method = PaymentMethod.Cash;
                    }

                    payments.Add(new CustomerOrderPayment(session.TenantId, order.Id, method, order.TotalAmount));
                }

                paymentPlans.Add((order, payments));
                result.MarkedCount++;
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or ArgumentNullException)
            {
                result.IgnoredCount++;
                result.IgnoredReasons.Add($"Pedido #{order.Number}: {exception.Message}");
            }
        }

        if (result.MarkedCount > 0)
        {
            var markedOrderIds = paymentPlans.Select(item => item.Order.Id).ToList();

            await _context.CustomerOrderPayments
                .Where(item => markedOrderIds.Contains(item.CustomerOrderId))
                .ExecuteDeleteAsync(cancellationToken);

            foreach (var (order, payments) in paymentPlans)
            {
                order.ReplacePayments(payments);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    public async Task DeleteOrderAsync(WorkspaceSessionContext session, Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _context.CustomerOrders
            .Include(item => item.Items)
                .ThenInclude(item => item.AdditionalSelections)
            .Include(item => item.DiningTable)
            .FirstOrDefaultAsync(
                item => item.Id == orderId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Pedido nao encontrado.");

        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            throw new InvalidOperationException("Pedidos pagos precisam ser apagados pelo fluxo protegido do caixa.");
        }

        await DeleteOrdersAsync(session, new[] { order }, "Apagado no caixa antes do pagamento.", cancellationToken);
    }

    public async Task DeletePaidOrderAsync(WorkspaceSessionContext session, Guid orderId, DeletePaidOrderRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Password);

        await ValidateOwnerPasswordAsync(session, request.Password, cancellationToken);

        var order = await _context.CustomerOrders
            .Include(item => item.Items)
                .ThenInclude(item => item.AdditionalSelections)
            .Include(item => item.DiningTable)
            .FirstOrDefaultAsync(
                item => item.Id == orderId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Pedido nao encontrado.");

        if (order.PaymentStatus != PaymentStatus.Paid)
        {
            throw new InvalidOperationException("So pedidos pagos podem ser apagados por aqui.");
        }

        await DeleteOrdersAsync(session, new[] { order }, "Apagado no caixa apos pagamento.", cancellationToken);
    }

    public async Task DeleteAllPaidOrdersAsync(WorkspaceSessionContext session, DeletePaidOrderRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Password);

        await ValidateOwnerPasswordAsync(session, request.Password, cancellationToken);

        var paidOrders = await _context.CustomerOrders
            .Include(item => item.Items)
                .ThenInclude(item => item.AdditionalSelections)
            .Include(item => item.DiningTable)
            .Where(item =>
                item.CompanyId == session.CompanyId &&
                item.IsActive &&
                item.PaymentStatus == PaymentStatus.Paid)
            .ToListAsync(cancellationToken);

        if (paidOrders.Count == 0)
        {
            return;
        }

        await DeleteOrdersAsync(session, paidOrders, "Limpeza de pedidos pagos no caixa.", cancellationToken);
    }

    public async Task DeleteClosedOrdersBatchAsync(WorkspaceSessionContext session, BatchDeleteClosedOrdersRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var orderIds = request.OrderIds
            .Where(item => item != Guid.Empty)
            .Distinct()
            .ToList();

        if (orderIds.Count == 0)
        {
            throw new ArgumentException("Selecione pelo menos um pedido encerrado.", nameof(request.OrderIds));
        }

        var orders = await _context.CustomerOrders
            .Include(item => item.Items)
                .ThenInclude(item => item.AdditionalSelections)
            .Include(item => item.DiningTable)
            .Where(item =>
                item.CompanyId == session.CompanyId &&
                item.IsActive &&
                orderIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

        if (orders.Count == 0)
        {
            return;
        }

        if (orders.Any(item => item.Status != OrderStatus.Cancelled && item.Status != OrderStatus.Delivered))
        {
            throw new InvalidOperationException("So pedidos encerrados podem ser removidos por aqui.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(request.Password);
        await ValidateOwnerPasswordAsync(session, request.Password, cancellationToken);

        await DeleteOrdersAsync(session, orders, "Remocao em lote de pedidos encerrados.", cancellationToken);
    }

    public async Task DeleteTodayOrderFlowAsync(WorkspaceSessionContext session, OwnerPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Password);

        await ValidateOwnerPasswordAsync(session, request.Password, cancellationToken);

        var company = await _context.Companies
            .FirstOrDefaultAsync(item => item.Id == session.CompanyId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");

        var orders = await _context.CustomerOrders
            .Include(item => item.Items)
                .ThenInclude(item => item.AdditionalSelections)
            .Include(item => item.DiningTable)
            .Where(item =>
                item.CompanyId == session.CompanyId &&
                item.IsActive)
            .ToListAsync(cancellationToken);

        if (orders.Count == 0)
        {
            company.ResetOrderNumberSequence();
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "SecurityAudit action={Action} company={CompanyId} user={UserId} affected={AffectedCount}",
                "reset-current-order-flow",
                session.CompanyId,
                session.UserId,
                0);
            return;
        }

        await ArchiveCurrentFlowOrdersAsync(session, orders, "Limpeza geral da operacao atual.", cancellationToken);

        company.ResetOrderNumberSequence();
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<GeneratedWorkspaceFile> GenerateDailyCashReportPdfAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        var timeZone = GetBusinessTimeZone();
        var (dayStartUtc, dayEndUtc, localNow) = GetCurrentBusinessDayRangeUtc(timeZone);

        var activeOrders = await _context.CustomerOrders
            .AsNoTracking()
            .Include(item => item.Items)
                .ThenInclude(item => item.AdditionalSelections)
            .Include(item => item.DiningTable)
            .Where(item =>
                item.CompanyId == session.CompanyId &&
                item.IsActive &&
                (
                    (item.SubmittedAtUtc >= dayStartUtc && item.SubmittedAtUtc < dayEndUtc) ||
                    (item.PaidAtUtc.HasValue && item.PaidAtUtc.Value >= dayStartUtc && item.PaidAtUtc.Value < dayEndUtc)
                ))
            .OrderBy(item => item.SubmittedAtUtc)
            .ThenBy(item => item.Number)
            .ToListAsync(cancellationToken);

        var deletedOrders = await _context.DeletedOrderRecords
            .AsNoTracking()
            .Where(item =>
                item.CompanyId == session.CompanyId &&
                item.DeletedAtUtc >= dayStartUtc &&
                item.DeletedAtUtc < dayEndUtc)
            .OrderBy(item => item.DeletedAtUtc)
            .ThenBy(item => item.OrderNumber)
            .ToListAsync(cancellationToken);

        var reportData = BuildDailyCashReportData(session, activeOrders, deletedOrders, timeZone, dayStartUtc, dayEndUtc, localNow);
        var pdfContent = new DailyCashReportDocument(reportData).GeneratePdf();

        return new GeneratedWorkspaceFile
        {
            Content = pdfContent,
            FileName = $"zeropaper-relatorio-caixa-{localNow:yyyy-MM-dd-HHmm}.pdf",
            ContentType = "application/pdf"
        };
    }

    public async Task<IReadOnlyList<StockItemDto>> GetStockItemsAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        var items = await _context.StockItems
            .AsNoTracking()
            .Where(item => item.CompanyId == session.CompanyId && item.IsActive)
            .OrderBy(item => item.CurrentQuantity <= item.MinimumQuantity)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

        return items.Select(MapStockItem).ToList();
    }

    public async Task<StockItemDto> CreateStockItemAsync(WorkspaceSessionContext session, SaveStockItemRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var item = new StockItem(
            session.TenantId,
            session.CompanyId,
            request.Name,
            request.Category,
            request.Unit,
            request.CurrentQuantity,
            request.MinimumQuantity);

        await _context.StockItems.AddAsync(item, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapStockItem(item);
    }

    public async Task<StockItemDto> UpdateStockItemAsync(WorkspaceSessionContext session, Guid stockItemId, SaveStockItemRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var item = await _context.StockItems.FirstOrDefaultAsync(
            stock => stock.Id == stockItemId &&
                     stock.CompanyId == session.CompanyId &&
                     stock.IsActive,
            cancellationToken)
            ?? throw new KeyNotFoundException("Stock item not found.");

        item.UpdateCatalog(request.Name, request.Category, request.Unit);
        item.UpdateStockLevels(request.CurrentQuantity, request.MinimumQuantity);

        await _context.SaveChangesAsync(cancellationToken);
        return MapStockItem(item);
    }

    public async Task<IReadOnlyList<TeamMemberDto>> GetTeamMembersAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        var members = await _context.Users
            .AsNoTracking()
            .Where(item => item.CompanyId == session.CompanyId && item.IsActive)
            .OrderBy(item => item.FullName)
            .ToListAsync(cancellationToken);

        return members.Select(item => new TeamMemberDto
        {
            Id = item.Id,
            FullName = item.FullName,
            Email = item.Email,
            Role = item.Role.ToString(),
            IsActive = item.IsActive,
            LastLoginAtUtc = item.LastLoginAtUtc
        }).ToList();
    }

    public async Task<TeamMemberDto> CreateTeamMemberAsync(WorkspaceSessionContext session, CreateTeamMemberRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Email);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Password);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailExists = await _context.Users.AnyAsync(
            item => item.TenantId == session.TenantId && item.Email == normalizedEmail,
            cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException("This email is already in use.");
        }

        var user = new AppUser(
            session.TenantId,
            session.CompanyId,
            request.FullName,
            normalizedEmail,
            _passwordHasher.Hash(request.Password),
            ParseUserRole(request.Role));

        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new TeamMemberDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            LastLoginAtUtc = user.LastLoginAtUtc
        };
    }

    public async Task<CompanySettingsDto> GetCompanySettingsAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == session.CompanyId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Company not found.");

        var user = await GetCurrentUserAsync(session, asNoTracking: true, cancellationToken);

        return MapCompanySettings(company, user, DateTime.UtcNow);
    }

    public async Task<CompanySettingsDto> UpdateCompanySettingsAsync(WorkspaceSessionContext session, UpdateCompanySettingsRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var company = await _context.Companies
            .FirstOrDefaultAsync(item => item.Id == session.CompanyId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Company not found.");

        company.UpdateNames(request.LegalName, request.TradeName);
        company.UpdateContact(request.ContactEmail, request.ContactPhone);

        await _context.SaveChangesAsync(cancellationToken);
        var user = await GetCurrentUserAsync(session, asNoTracking: true, cancellationToken);
        return MapCompanySettings(company, user, DateTime.UtcNow);
    }

    public async Task<CompanySettingsDto> UploadCompanyLogoAsync(WorkspaceSessionContext session, IFormFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length <= 0)
        {
            throw new ArgumentException("Selecione uma imagem valida.", nameof(file));
        }

        if (file.Length > 3 * 1024 * 1024)
        {
            throw new ArgumentException("A logo precisa ter no maximo 3 MB.", nameof(file));
        }

        var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        if (!allowedTypes.Contains(file.ContentType))
        {
            throw new ArgumentException("Use uma logo JPG, PNG ou WEBP.", nameof(file));
        }

        var company = await _context.Companies
            .FirstOrDefaultAsync(item => item.Id == session.CompanyId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Company not found.");

        var safeExtension = await SafeUploadValidator.GetImageExtensionAsync(file, cancellationToken);

        var relativeDirectory = Path.Combine("logos", session.CompanyId.ToString("N"));
        var directoryPath = Path.Combine(_uploadsRootPath, relativeDirectory);
        Directory.CreateDirectory(directoryPath);

        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var filePath = Path.Combine(directoryPath, fileName);

        await using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var relativePath = "/" + Path.Combine("uploads", relativeDirectory, fileName).Replace("\\", "/");
        company.UpdateLogo(relativePath);

        await _context.SaveChangesAsync(cancellationToken);
        var user = await GetCurrentUserAsync(session, asNoTracking: true, cancellationToken);
        return MapCompanySettings(company, user, DateTime.UtcNow);
    }

    public async Task<CompanySettingsDto> ResetCompanyLogoAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(item => item.Id == session.CompanyId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Company not found.");

        company.UpdateLogo(null);

        await _context.SaveChangesAsync(cancellationToken);
        var user = await GetCurrentUserAsync(session, asNoTracking: true, cancellationToken);
        return MapCompanySettings(company, user, DateTime.UtcNow);
    }

    public async Task<OwnerProfileDto> GetOwnerProfileAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == session.UserId &&
                        item.TenantId == session.TenantId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Perfil do owner nao encontrado.");

        return MapOwnerProfile(user);
    }

    public async Task<OwnerProfileDto> UpdateOwnerProfileAsync(WorkspaceSessionContext session, UpdateOwnerProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fullName = NormalizeOwnerFullName(request.FullName);
        var email = NormalizeOwnerEmail(request.Email);

        var emailExists = await _context.Users.AnyAsync(
            item => item.TenantId == session.TenantId &&
                    item.Email == email &&
                    item.Id != session.UserId,
            cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException("Este email ja esta em uso por outro usuario.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(
                item => item.Id == session.UserId &&
                        item.TenantId == session.TenantId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Perfil do owner nao encontrado.");

        user.ChangeIdentity(fullName, email);
        await _context.SaveChangesAsync(cancellationToken);

        return MapOwnerProfile(user);
    }

    public async Task ChangeOwnerPasswordAsync(WorkspaceSessionContext session, ChangeOwnerPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            throw new ArgumentException("Informe a senha atual.", nameof(request.CurrentPassword));
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ArgumentException("Informe a nova senha.", nameof(request.NewPassword));
        }

        if (request.NewPassword.Trim().Length < 8)
        {
            throw new ArgumentException("A nova senha precisa ter pelo menos 8 caracteres.", nameof(request.NewPassword));
        }

        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            throw new ArgumentException("A confirmacao da nova senha nao confere.", nameof(request.ConfirmPassword));
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(
                item => item.Id == session.UserId &&
                        item.TenantId == session.TenantId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Perfil do owner nao encontrado.");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Senha atual incorreta.");
        }

        if (_passwordHasher.Verify(request.NewPassword, user.PasswordHash))
        {
            throw new ArgumentException("A nova senha precisa ser diferente da senha atual.", nameof(request.NewPassword));
        }

        user.ChangePasswordHash(_passwordHasher.Hash(request.NewPassword));
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "SecurityAudit action={Action} company={CompanyId} user={UserId}",
            "change-owner-password",
            session.CompanyId,
            session.UserId);
    }

    public async Task<GenerateOwnerShortcutAccessResponseDto> RotateOwnerShortcutAccessAsync(
        WorkspaceSessionContext session,
        GenerateOwnerShortcutAccessRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureOwnerShortcutAllowed(session);

        var user = await GetCurrentUserAsync(session, asNoTracking: false, cancellationToken);
        VerifyOwnerShortcutPassword(request.Password, user);

        var utcNow = DateTime.UtcNow;
        var expiresAtUtc = utcNow.AddDays(30);
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(48)).ToLowerInvariant();
        user.RotateShortcutAccessToken(ComputeShortcutAccessTokenHash(rawToken), utcNow, expiresAtUtc);

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "SecurityAudit action={Action} company={CompanyId} user={UserId} expiresAt={ExpiresAtUtc}",
            "rotate-owner-shortcut",
            session.CompanyId,
            session.UserId,
            expiresAtUtc);

        return new GenerateOwnerShortcutAccessResponseDto
        {
            ShortcutAccess = MapOwnerShortcutAccess(user, utcNow),
            RawToken = rawToken,
            ShortcutUrl = BuildOwnerShortcutUrl(rawToken)
        };
    }

    public async Task<OwnerShortcutAccessDto> RevokeOwnerShortcutAccessAsync(
        WorkspaceSessionContext session,
        GenerateOwnerShortcutAccessRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureOwnerShortcutAllowed(session);

        var user = await GetCurrentUserAsync(session, asNoTracking: false, cancellationToken);
        VerifyOwnerShortcutPassword(request.Password, user);

        user.RevokeShortcutAccess(DateTime.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "SecurityAudit action={Action} company={CompanyId} user={UserId}",
            "revoke-owner-shortcut",
            session.CompanyId,
            session.UserId);

        return MapOwnerShortcutAccess(user, DateTime.UtcNow);
    }

    public async Task<AlertSettingsDto> UpdateAlertSettingsAsync(WorkspaceSessionContext session, UpdateAlertSettingsRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var company = await _context.Companies
            .FirstOrDefaultAsync(item => item.Id == session.CompanyId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Company not found.");

        company.UpdateAlertPreferences(
            request.EnableOrderAlerts,
            request.EnableWaiterCallAlerts,
            request.VolumePercent,
            request.PlaybackSeconds);
        await _context.SaveChangesAsync(cancellationToken);

        return MapAlertSettings(company);
    }

    public async Task<UploadAlertSoundResponseDto> UploadAlertSoundAsync(WorkspaceSessionContext session, IFormFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length <= 0)
        {
            throw new ArgumentException("Selecione um audio valido.", nameof(file));
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            throw new ArgumentException("O audio precisa ter no maximo 10 MB.", nameof(file));
        }

        var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "audio/wav",
            "audio/x-wav",
            "audio/wave",
            "audio/mpeg",
            "audio/mp3",
            "audio/ogg"
        };

        if (!allowedTypes.Contains(file.ContentType))
        {
            throw new ArgumentException("Use um audio WAV, MP3 ou OGG.", nameof(file));
        }

        var safeExtension = await SafeUploadValidator.GetAudioExtensionAsync(file, cancellationToken);

        var relativeDirectory = Path.Combine("alerts", session.CompanyId.ToString("N"));
        var directoryPath = Path.Combine(_uploadsRootPath, relativeDirectory);
        Directory.CreateDirectory(directoryPath);

        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var filePath = Path.Combine(directoryPath, fileName);

        await using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var relativePath = "/" + Path.Combine("uploads", relativeDirectory, fileName).Replace("\\", "/");

        var company = await _context.Companies
            .FirstOrDefaultAsync(item => item.Id == session.CompanyId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Company not found.");

        company.UpdateAlertSound(relativePath);
        await _context.SaveChangesAsync(cancellationToken);

        return new UploadAlertSoundResponseDto
        {
            Alerts = MapAlertSettings(company)
        };
    }

    public async Task<AlertSettingsDto> ResetAlertSoundAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(item => item.Id == session.CompanyId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Company not found.");

        company.UpdateAlertSound(null);
        await _context.SaveChangesAsync(cancellationToken);

        return MapAlertSettings(company);
    }

    public async Task<DeliveryFreightSettingsDto> GetDeliveryFreightSettingsAsync(
        WorkspaceSessionContext session,
        CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == session.CompanyId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");

        return _deliveryFreightService.BuildSettings(company);
    }

    public async Task<DeliveryFreightSettingsDto> UpdateDeliveryFreightSettingsAsync(
        WorkspaceSessionContext session,
        UpdateDeliveryFreightSettingsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Password);

        await ValidateOwnerPasswordAsync(session, request.Password, cancellationToken);

        var company = await _context.Companies
            .FirstOrDefaultAsync(item => item.Id == session.CompanyId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");

        company.UpdateDeliveryFreightSettings(
            request.IsEnabled,
            request.OriginPostalCode,
            request.PricePerKm,
            request.BaseFee,
            request.BaseDistanceKm,
            request.PickupEstimatedMinutes,
            request.DeliveryEstimatedMinutes);

        await _context.SaveChangesAsync(cancellationToken);
        return _deliveryFreightService.BuildSettings(company);
    }

    public async Task<IReadOnlyList<WaiterCallDto>> GetWaiterCallsAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        var calls = await _context.WaiterCalls
            .AsNoTracking()
            .Include(item => item.DiningTable)
            .Where(item =>
                item.CompanyId == session.CompanyId &&
                item.IsActive &&
                item.ResolvedAtUtc == null)
            .OrderByDescending(item => item.RequestedAtUtc)
            .ToListAsync(cancellationToken);

        return calls.Select(item => MapWaiterCall(item)).ToList();
    }

    public async Task<WorkspaceAlertsSignalDto> GetAlertsSignalAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        var latestWaiterCall = await _context.WaiterCalls
            .AsNoTracking()
            .Include(item => item.DiningTable)
            .Where(item =>
                item.CompanyId == session.CompanyId &&
                item.IsActive &&
                item.ResolvedAtUtc == null)
            .OrderByDescending(item => item.RequestedAtUtc)
            .Select(item => new
            {
                item.RequestedAtUtc,
                TableName = item.DiningTable.Name,
                item.DiningTable.AlertSoundUrl
            })
            .FirstOrDefaultAsync(cancellationToken);

        var pendingWaiterCalls = latestWaiterCall is null
            ? 0
            : await _context.WaiterCalls.CountAsync(
                item =>
                    item.CompanyId == session.CompanyId &&
                    item.IsActive &&
                    item.ResolvedAtUtc == null,
                cancellationToken);

        var latestOrderAtUtc = await _context.CustomerOrders
            .AsNoTracking()
            .Where(item =>
                item.CompanyId == session.CompanyId &&
                item.IsActive &&
                item.Status != OrderStatus.Delivered &&
                item.Status != OrderStatus.Cancelled)
            .MaxAsync(item => (DateTime?)item.SubmittedAtUtc, cancellationToken);

        return new WorkspaceAlertsSignalDto
        {
            PendingWaiterCalls = pendingWaiterCalls,
            LatestWaiterCallAtUtc = latestWaiterCall?.RequestedAtUtc,
            LatestWaiterCallTableName = latestWaiterCall?.TableName,
            LatestWaiterCallTableSoundUrl = latestWaiterCall?.AlertSoundUrl,
            LatestOrderAtUtc = latestOrderAtUtc
        };
    }

    public async Task<WaiterCallDto> ResolveWaiterCallAsync(WorkspaceSessionContext session, Guid waiterCallId, CancellationToken cancellationToken = default)
    {
        var waiterCall = await _context.WaiterCalls
            .Include(item => item.DiningTable)
            .FirstOrDefaultAsync(
                item => item.Id == waiterCallId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Chamada nao encontrada.");

        waiterCall.Resolve();
        await _context.SaveChangesAsync(cancellationToken);

        return MapWaiterCall(waiterCall);
    }

    public async Task<PublicTableViewDto> GetPublicTableAsync(string publicCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicCode);

        var normalizedCode = publicCode.Trim().ToLowerInvariant();

        var table = await _context.DiningTables
            .Include(item => item.Company)
            .Include(item => item.QrCodeAccess)
            .FirstOrDefaultAsync(
                item => item.QrCodeAccess.PublicCode == normalizedCode &&
                        item.IsActive &&
                        item.QrCodeAccess.IsActive &&
                        item.Status != TableStatus.Inactive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Table not found.");

        var activeSubscription = await GetActivePublicSubscriptionAsync(table.TenantId, cancellationToken);
        EnsurePublicOrderingEnabled(table, activeSubscription);

        table.QrCodeAccess.RegisterScan();
        await _context.SaveChangesAsync(cancellationToken);
        var orderingStatus = BuildPublicOrderingStatus(table.Company);

        return new PublicTableViewDto
        {
            RestaurantName = table.Company.TradeName,
            RestaurantLogoUrl = NormalizeMenuImagePath(table.Company.LogoUrl),
            TableName = table.Name,
            AccessCode = table.QrCodeAccess.PublicCode,
            IsDeliveryChannel = table.IsDeliveryChannel,
            IsOnlinePaymentAvailable = table.Company.IsMercadoPagoConnected,
            DeliveryEditWindowMinutes = 0,
            IsOrderingAvailable = orderingStatus.IsOpen,
            OrderingUnavailableMessage = orderingStatus.IsOpen ? null : orderingStatus.Message,
            ServiceDays = ParsePublicServiceDays(table.Company.AiAssistantServiceDays),
            ServiceStartTime = table.Company.AiAssistantServiceStartTime,
            ServiceEndTime = table.Company.AiAssistantServiceEndTime,
            PickupEstimatedMinutes = table.Company.PickupEstimatedMinutes,
            DeliveryEstimatedMinutes = table.Company.DeliveryEstimatedMinutes,
            Menu = await BuildPublicMenuAsync(table.CompanyId, cancellationToken)
        };
    }

    public async Task<MenuItemDto> GetPublicMenuItemAsync(string publicCode, Guid menuItemId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicCode);

        var normalizedCode = publicCode.Trim().ToLowerInvariant();
        var table = await _context.DiningTables
            .AsNoTracking()
            .Include(item => item.Company)
            .Include(item => item.QrCodeAccess)
            .FirstOrDefaultAsync(
                item => item.QrCodeAccess.PublicCode == normalizedCode &&
                        item.IsActive &&
                        item.QrCodeAccess.IsActive &&
                        item.Status != TableStatus.Inactive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Table not found.");

        var activeSubscription = await GetActivePublicSubscriptionAsync(table.TenantId, cancellationToken);
        EnsurePublicOrderingEnabled(table, activeSubscription);

        var menuItem = await _context.MenuItems
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.AdditionalGroups)
                .ThenInclude(item => item.Options)
            .FirstOrDefaultAsync(
                item => item.Id == menuItemId &&
                        item.CompanyId == table.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Menu item not found.");

        return MapMenuItem(menuItem);
    }

    public async Task<CustomerOrderDto> CreatePublicOrderAsync(string publicCode, CreateCustomerOrderRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(publicCode);

        var normalizedCode = publicCode.Trim().ToLowerInvariant();

        var table = await _context.DiningTables
            .Include(item => item.QrCodeAccess)
            .Include(item => item.Company)
            .FirstOrDefaultAsync(
                item => item.QrCodeAccess.PublicCode == normalizedCode &&
                        item.IsActive &&
                        item.QrCodeAccess.IsActive &&
                        item.Status != TableStatus.Inactive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Table not found.");

        var activeSubscription = await GetActivePublicSubscriptionAsync(table.TenantId, cancellationToken);
        EnsurePublicOrderingEnabled(table, activeSubscription);
        EnsurePublicOrderingOpen(table.Company);

        if (!string.IsNullOrWhiteSpace(request.CouponCode) &&
            !CommercialPlanCatalog.ResolveFeatures(activeSubscription?.PlanName).HasCoupons)
        {
            throw new InvalidOperationException("Cupons nao fazem parte do plano atual da unidade.");
        }

        return await CreateOrderForTableAsync(
            table,
            request.CustomerName,
            request.Notes,
            request.DeliveryPhone,
            request.DeliveryAddress,
            request.DeliveryNumber,
            request.DeliveryNeighborhood,
            request.DeliveryComplement,
            request.DeliveryPostalCode,
            request.FulfillmentType,
            request.Items,
            request.MenuSelections,
            ParsePaymentMethodOrDefault(request.PaymentMethod),
            request.CouponCode,
            cancellationToken);
    }

    public async Task<CustomerOrderDto> CreateSellerLinkOrderAsync(
        Guid salesAgentId,
        Guid tenantId,
        Guid companyId,
        CreateCustomerOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cashTable = await _cashOrderTableService.EnsureAsync(tenantId, companyId, cancellationToken);

        return await CreateOrderForTableAsync(
            cashTable,
            request.CustomerName,
            request.Notes,
            request.DeliveryPhone,
            request.DeliveryAddress,
            request.DeliveryNumber,
            request.DeliveryNeighborhood,
            request.DeliveryComplement,
            request.DeliveryPostalCode,
            request.FulfillmentType,
            request.Items,
            request.MenuSelections,
            ParsePaymentMethodOrDefault(request.PaymentMethod),
            request.CouponCode,
            cancellationToken,
            salesAgentId);
    }

    public async Task<DeliveryFreightQuoteDto> QuotePublicDeliveryFreightAsync(
        string publicCode,
        DeliveryFreightQuoteRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(publicCode);

        var normalizedCode = publicCode.Trim().ToLowerInvariant();
        var table = await _context.DiningTables
            .Include(item => item.QrCodeAccess)
            .Include(item => item.Company)
            .FirstOrDefaultAsync(
                item => item.QrCodeAccess.PublicCode == normalizedCode &&
                        item.IsActive &&
                        item.IsDeliveryChannel &&
                        item.QrCodeAccess.IsActive &&
                        item.Status != TableStatus.Inactive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Canal de delivery nao encontrado.");

        var activeSubscription = await GetActivePublicSubscriptionAsync(table.TenantId, cancellationToken);
        EnsurePublicOrderingEnabled(table, activeSubscription);
        EnsurePublicOrderingOpen(table.Company);

        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == table.CompanyId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");

        var quote = await _deliveryFreightService.QuoteAsync(
            company,
            request.DestinationPostalCode,
            request.Subtotal,
            cancellationToken);

        return HidePublicFreightQuoteDetails(quote);
    }

    public async Task<PublicDeliveryCustomerProfileDto> GetPublicDeliveryCustomerProfileAsync(
        string publicCode,
        string? token,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicCode);

        var normalizedCode = publicCode.Trim().ToLowerInvariant();
        var table = await _context.DiningTables
            .AsNoTracking()
            .Include(item => item.QrCodeAccess)
            .FirstOrDefaultAsync(
                item => item.QrCodeAccess.PublicCode == normalizedCode &&
                        item.IsActive &&
                        item.IsDeliveryChannel &&
                        item.QrCodeAccess.IsActive &&
                        item.Status != TableStatus.Inactive,
                cancellationToken);

        DeliveryCustomerLinkPayload? payload = null;
        var legacyTokenIsValid = _deliveryCustomerLinkService.TryReadToken(token, out var legacyPayload);
        if (legacyTokenIsValid)
        {
            payload = legacyPayload;
        }
        else
        {
            payload = await _deliveryCustomerLinkService.TryReadShortCodeAsync(token, cancellationToken);
        }

        if (table is null ||
            payload is null ||
            payload.CompanyId != table.CompanyId ||
            (legacyTokenIsValid && !string.Equals(payload.PublicCode, normalizedCode, StringComparison.OrdinalIgnoreCase)))
        {
            return new PublicDeliveryCustomerProfileDto
            {
                Found = false,
                Message = "Este link nao trouxe dados salvos. Use o link mais recente recebido no WhatsApp ou preencha a entrega normalmente."
            };
        }

        var activeSubscription = await GetActivePublicSubscriptionAsync(table.TenantId, cancellationToken);
        EnsurePublicOrderingEnabled(table, activeSubscription);

        var normalizedPhone = DeliveryCustomerProfile.NormalizePhone(payload.Phone);
        var profile = await _context.DeliveryCustomerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item =>
                    item.CompanyId == table.CompanyId &&
                    item.Phone == normalizedPhone &&
                    item.IsActive,
                cancellationToken);

        if (profile is not null)
        {
            return new PublicDeliveryCustomerProfileDto
            {
                Found = true,
                CustomerName = profile.CustomerName,
                DeliveryPhone = profile.Phone,
                DeliveryAddress = profile.DeliveryAddress,
                DeliveryNumber = profile.DeliveryNumber,
                DeliveryNeighborhood = profile.DeliveryNeighborhood,
                DeliveryComplement = profile.DeliveryComplement,
                DeliveryPostalCode = profile.DeliveryPostalCode,
                LastOrderAtUtc = profile.LastOrderAtUtc,
                Message = $"Usamos os dados do seu ultimo delivery em {profile.LastOrderAtUtc.ToString("dd/MM/yyyy", PtBrCulture)}. Confira o endereco e altere se precisar."
            };
        }

        var recentOrders = await _context.CustomerOrders
            .AsNoTracking()
            .Include(item => item.DiningTable)
            .Where(item =>
                item.CompanyId == table.CompanyId &&
                item.IsActive &&
                item.DiningTable.IsDeliveryChannel &&
                item.DeliveryPhone != null)
            .OrderByDescending(item => item.SubmittedAtUtc)
            .Take(250)
            .ToListAsync(cancellationToken);

        var lastOrder = recentOrders.FirstOrDefault(item =>
            string.Equals(
                NormalizePhoneForDeliveryCustomer(item.DeliveryPhone),
                normalizedPhone,
                StringComparison.Ordinal));

        if (lastOrder is null)
        {
            return new PublicDeliveryCustomerProfileDto
            {
                Found = false,
                DeliveryPhone = normalizedPhone,
                Message = "Ainda nao encontramos um pedido anterior neste numero. Preencha a entrega uma vez para salvar o fluxo."
            };
        }

        var lastOrderWasPickup = IsPickupOrder(lastOrder);
        return new PublicDeliveryCustomerProfileDto
        {
            Found = true,
            CustomerName = lastOrder.CustomerName,
            DeliveryPhone = lastOrder.DeliveryPhone,
            DeliveryAddress = lastOrderWasPickup ? null : lastOrder.DeliveryAddress,
            DeliveryNumber = lastOrderWasPickup ? null : lastOrder.DeliveryNumber,
            DeliveryNeighborhood = null,
            DeliveryComplement = lastOrderWasPickup ? null : lastOrder.DeliveryComplement,
            DeliveryPostalCode = lastOrderWasPickup ? null : lastOrder.DeliveryPostalCode,
            LastOrderAtUtc = lastOrder.SubmittedAtUtc,
            Message = $"Usamos os dados do seu ultimo pedido em {lastOrder.SubmittedAtUtc.ToString("dd/MM/yyyy", PtBrCulture)}. Confira o endereco e altere se precisar."
        };
    }

    public async Task<PublicCustomerProfileDto> GetPublicCustomerProfileAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var payload = await _deliveryCustomerLinkService.TryReadShortCodeAsync(code, cancellationToken);
        if (payload is null)
        {
            return new PublicCustomerProfileDto
            {
                Found = false,
                Message = "Perfil do cliente nao encontrado ou link invalido."
            };
        }

        var normalizedPhone = DeliveryCustomerProfile.NormalizePhone(payload.Phone);
        var profile = await _context.DeliveryCustomerProfiles
            .AsNoTracking()
            .Include(item => item.Company)
            .FirstOrDefaultAsync(
                item =>
                    item.CompanyId == payload.CompanyId &&
                    item.Phone == normalizedPhone &&
                    item.IsActive &&
                    item.Company.IsActive,
                cancellationToken);

        if (profile is null)
        {
            return new PublicCustomerProfileDto
            {
                Found = false,
                Message = "Perfil do cliente nao encontrado ou link invalido."
            };
        }

        var recentHistory = await _context.CustomerOrderHistories
            .AsNoTracking()
            .Include(item => item.Items)
            .Where(item =>
                item.CompanyId == profile.CompanyId &&
                item.CustomerProfileId == profile.Id &&
                item.IsActive)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(5)
            .ToListAsync(cancellationToken);

        var orderIds = recentHistory.Select(item => item.OrderId).ToList();
        var orders = await _context.CustomerOrders
            .AsNoTracking()
            .Include(item => item.DiningTable)
            .Include(item => item.Items)
            .Where(item =>
                item.CompanyId == profile.CompanyId &&
                item.IsActive &&
                orderIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        var recentOrders = recentHistory
            .Select(history =>
            {
                orders.TryGetValue(history.OrderId, out var order);
                return MapPublicCustomerRecentOrder(history, order);
            })
            .ToList();

        return new PublicCustomerProfileDto
        {
            Found = true,
            CustomerName = profile.CustomerName,
            MaskedPhone = MaskCustomerPhone(profile.Phone),
            PrimaryAddress = MapPublicCustomerPrimaryAddress(profile),
            BusinessName = profile.Company.TradeName,
            BusinessSlug = profile.Company.AccessSlug,
            CanEditProfile = false,
            CanReorder = false,
            HasActiveOrder = recentOrders.Any(item =>
                string.Equals(item.Status, OrderStatus.Pending.ToString(), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.Status, OrderStatus.InKitchen.ToString(), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.Status, OrderStatus.Ready.ToString(), StringComparison.OrdinalIgnoreCase)),
            RecentOrders = recentOrders,
            Message = recentOrders.Count == 0
                ? "Perfil localizado. Ainda nao ha pedidos recentes para exibir."
                : "Perfil localizado."
        };
    }

    public async Task<PublicDeliveryShortLinkDto> ResolvePublicDeliveryShortLinkAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var payload = await _deliveryCustomerLinkService.TryReadShortCodeAsync(code, cancellationToken);
        if (payload is null)
        {
            return new PublicDeliveryShortLinkDto
            {
                Found = false,
                Message = "Link de delivery nao encontrado ou expirado."
            };
        }

        var table = await _context.DiningTables
            .AsNoTracking()
            .Include(item => item.QrCodeAccess)
            .FirstOrDefaultAsync(
                item =>
                    item.CompanyId == payload.CompanyId &&
                    item.IsActive &&
                    item.IsDeliveryChannel &&
                    item.QrCodeAccess != null &&
                    item.QrCodeAccess.IsActive &&
                    item.Status != TableStatus.Inactive,
                cancellationToken);

        if (table?.QrCodeAccess is null)
        {
            return new PublicDeliveryShortLinkDto
            {
                Found = false,
                Message = "Canal de delivery nao encontrado para este link."
            };
        }

        return new PublicDeliveryShortLinkDto
        {
            Found = true,
            PublicCode = table.QrCodeAccess.PublicCode.Trim().ToLowerInvariant(),
            CustomerToken = code.Trim(),
            Message = "Link seguro de delivery localizado."
        };
    }

    public async Task<PublicOrderTrackingDto> GetPublicDeliveryTrackingAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var payload = await _deliveryCustomerLinkService.TryReadShortCodeAsync(code, cancellationToken);
        if (payload is null)
        {
            return new PublicOrderTrackingDto
            {
                Found = false,
                Message = "Link de acompanhamento nao encontrado."
            };
        }

        var recentOrders = await _context.CustomerOrders
            .AsNoTracking()
            .Include(item => item.Company)
            .Include(item => item.DiningTable)
            .Include(item => item.Items)
                .ThenInclude(item => item.AdditionalSelections)
            .Where(item =>
                item.CompanyId == payload.CompanyId &&
                item.IsActive &&
                item.DiningTable.IsDeliveryChannel &&
                item.Status != OrderStatus.Cancelled &&
                item.DeliveryPhone != null)
            .OrderByDescending(item => item.SubmittedAtUtc)
            .Take(250)
            .ToListAsync(cancellationToken);

        var order = recentOrders.FirstOrDefault(item =>
            string.Equals(
                NormalizePhoneForDeliveryCustomer(item.DeliveryPhone),
                payload.Phone,
                StringComparison.Ordinal));

        if (order is null)
        {
            return new PublicOrderTrackingDto
            {
                Found = false,
                Message = "Nao encontrei nenhum pedido para este numero."
            };
        }

        return new PublicOrderTrackingDto
        {
            Found = true,
            RestaurantName = order.Company.TradeName,
            Message = "Pedido localizado.",
            Order = new PublicTrackedOrderDto
            {
                Number = order.Number,
                Status = order.Status.ToString(),
                PaymentStatus = order.PaymentStatus.ToString(),
                PaymentMethod = order.PaymentMethod.ToString(),
                TotalAmount = order.TotalAmount,
                TotalItemQuantity = order.Items.Sum(item => item.Quantity),
                SubmittedAtUtc = AsUtc(order.SubmittedAtUtc),
                IsEdited = order.IsEdited,
                EditedAtUtc = AsUtc(order.EditedAtUtc),
                Items = order.Items
                    .OrderBy(item => item.CreatedAtUtc)
                    .Select(item => new PublicTrackedOrderItemDto
                    {
                        Name = item.Name,
                        Quantity = item.Quantity,
                        TotalPrice = item.TotalPrice,
                        Notes = item.Notes,
                        AdditionalSelections = item.AdditionalSelections
                            .Select(selection => new OrderItemAdditionalSelectionDto
                            {
                                SourceMenuItemAdditionalOptionId = selection.SourceMenuItemAdditionalOptionId,
                                GroupName = selection.GroupName,
                                OptionName = selection.OptionName,
                                UnitPrice = selection.UnitPrice
                            })
                            .ToList()
                    })
                    .ToList()
            }
        };
    }

    public async Task<WaiterCallDto> CreatePublicWaiterCallAsync(string publicCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicCode);

        var normalizedCode = publicCode.Trim().ToLowerInvariant();

        var table = await _context.DiningTables
            .Include(item => item.QrCodeAccess)
            .FirstOrDefaultAsync(
                item => item.QrCodeAccess.PublicCode == normalizedCode &&
                        item.IsActive &&
                        item.QrCodeAccess.IsActive &&
                        item.Status != TableStatus.Inactive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Table not found.");

        var activeSubscription = await GetActivePublicSubscriptionAsync(table.TenantId, cancellationToken);
        EnsurePublicWaiterCallEnabled(table, activeSubscription);

        var waiterCall = await _context.WaiterCalls
            .Include(item => item.DiningTable)
            .FirstOrDefaultAsync(
                item => item.DiningTableId == table.Id &&
                        item.CompanyId == table.CompanyId &&
                        item.IsActive &&
                        item.ResolvedAtUtc == null,
                cancellationToken);

        if (table.IsDeliveryChannel)
        {
            throw new InvalidOperationException("Chamado de atendente nao se aplica ao canal de delivery.");
        }

        if (waiterCall is null)
        {
            waiterCall = new WaiterCall(table.TenantId, table.CompanyId, table.Id);
            await _context.WaiterCalls.AddAsync(waiterCall, cancellationToken);
        }
        else
        {
            waiterCall.Repeat();
        }

        await _context.SaveChangesAsync(cancellationToken);
        return MapWaiterCall(waiterCall, table.Name);
    }

    private async Task<CustomerOrderDto> CreateOrderForTableAsync(
        DiningTable table,
        string? customerName,
        string? notes,
        string? deliveryPhone,
        string? deliveryAddress,
        string? deliveryNumber,
        string? deliveryNeighborhood,
        string? deliveryComplement,
        string? deliveryPostalCode,
        string? fulfillmentType,
        List<OrderItemInputDto> items,
        List<MenuOrderSelectionDto> menuSelections,
        PaymentMethod paymentMethod,
        string? couponCode,
        CancellationToken cancellationToken,
        Guid? salesAgentId = null)
    {
        var resolvedFulfillmentType = ResolveFulfillmentType(table, fulfillmentType);
        var isPickupOrder = resolvedFulfillmentType == FulfillmentTypePickup;

        if (table.IsDeliveryChannel)
        {
            if (string.IsNullOrWhiteSpace(customerName))
            {
                throw new ArgumentException("Informe o nome para o delivery.", nameof(customerName));
            }

            if (string.IsNullOrWhiteSpace(deliveryPhone))
            {
                throw new ArgumentException("Informe um telefone para a entrega.", nameof(deliveryPhone));
            }

            if (!isPickupOrder && paymentMethod == PaymentMethod.Undefined)
            {
                throw new ArgumentException("Escolha a forma de pagamento para a entrega.", nameof(paymentMethod));
            }

            if (!isPickupOrder && string.IsNullOrWhiteSpace(deliveryAddress))
            {
                throw new ArgumentException("Informe o endereco da entrega.", nameof(deliveryAddress));
            }

            if (!isPickupOrder && string.IsNullOrWhiteSpace(deliveryNumber))
            {
                throw new ArgumentException("Informe o numero do endereco.", nameof(deliveryNumber));
            }

            if (!isPickupOrder && string.IsNullOrWhiteSpace(deliveryPostalCode))
            {
                throw new ArgumentException("Informe o CEP da entrega.", nameof(deliveryPostalCode));
            }
        }

        var orderItems = await BuildOrderItemsAsync(
            table.CompanyId,
            table.TenantId,
            items,
            menuSelections,
            cancellationToken);

        if (orderItems.Count == 0)
        {
            throw new ArgumentException("At least one item must be informed.", nameof(items));
        }

        var order = await PersistOrderAsync(
            table,
            customerName,
            notes,
            deliveryPhone,
            deliveryAddress,
            deliveryNumber,
            deliveryComplement,
            deliveryPostalCode,
            resolvedFulfillmentType,
            orderItems,
            paymentMethod,
            couponCode,
            cancellationToken,
            salesAgentId);

        await UpsertDeliveryCustomerProfileAsync(order, deliveryNeighborhood, cancellationToken);
        var publicDeliveryCustomerUrl = await BuildPublicDeliveryCustomerUrlAsync(order, cancellationToken);
        var response = MapOrder(
            order,
            table.Name,
            publicEditUrl: null,
            BuildDeliveryAssistantMessage(order),
            includeDeliveryDistance: false,
            publicDeliveryCustomerUrl: publicDeliveryCustomerUrl);
        await TryNotifyDeliveryOrderAsync(order.Id, isUpdate: false, cancellationToken);
        return response;
    }

    private async Task<EditedOrderContext> BuildEditedOrderContextAsync(
        CustomerOrder order,
        UpdateCustomerOrderRequestDto request,
        IReadOnlyCollection<OrderItem> updatedItems,
        CancellationToken cancellationToken)
    {
        var paymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod)
            ? order.RequestedPaymentMethod
            : ParsePaymentMethodOrDefault(request.PaymentMethod);

        if (!order.DiningTable.IsDeliveryChannel)
        {
            return new EditedOrderContext(
                null,
                null,
                null,
                null,
                null,
                0m,
                null,
                null,
                paymentMethod);
        }

        var resolvedFulfillmentType = ResolveFulfillmentType(order.DiningTable, request.FulfillmentType);
        var isPickupOrder = resolvedFulfillmentType == FulfillmentTypePickup;

        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            throw new ArgumentException("Informe o nome para o delivery.", nameof(request.CustomerName));
        }

        if (string.IsNullOrWhiteSpace(request.DeliveryPhone))
        {
            throw new ArgumentException("Informe um telefone para a entrega.", nameof(request.DeliveryPhone));
        }

        if (isPickupOrder)
        {
            return new EditedOrderContext(
                request.DeliveryPhone,
                PickupAddressMarker,
                null,
                null,
                null,
                0m,
                null,
                null,
                paymentMethod);
        }

        if (paymentMethod == PaymentMethod.Undefined)
        {
            throw new ArgumentException("Escolha a forma de pagamento para a entrega.", nameof(request.PaymentMethod));
        }

        if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
        {
            throw new ArgumentException("Informe o endereco da entrega.", nameof(request.DeliveryAddress));
        }

        if (string.IsNullOrWhiteSpace(request.DeliveryNumber))
        {
            throw new ArgumentException("Informe o numero do endereco.", nameof(request.DeliveryNumber));
        }

        var normalizedPostalCode = NormalizePostalCodeOrThrow(request.DeliveryPostalCode, nameof(request.DeliveryPostalCode));
        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == order.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Company not found.");
        var freightQuote = await _deliveryFreightService.QuoteAsync(
            company,
            normalizedPostalCode,
            updatedItems.Sum(item => item.TotalPrice),
            cancellationToken);

        return new EditedOrderContext(
            request.DeliveryPhone,
            request.DeliveryAddress,
            request.DeliveryNumber,
            request.DeliveryComplement,
            normalizedPostalCode,
            freightQuote.IsAvailable ? freightQuote.FreightAmount : 0m,
            freightQuote.IsAvailable ? freightQuote.DistanceKm : null,
            freightQuote.IsAvailable ? freightQuote.Provider : null,
            paymentMethod);
    }

    private async Task<Subscription?> GetActivePublicSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _context.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.TenantId == tenantId &&
                        item.IsActive &&
                        (item.Status == SubscriptionStatus.Active || item.Status == SubscriptionStatus.Trial),
                cancellationToken);
    }

    private static void EnsurePublicOrderingEnabled(DiningTable table, Subscription? activeSubscription)
    {
        var includesTables = activeSubscription?.IncludesTablesModule ?? true;
        var includesDelivery = activeSubscription?.IncludesDeliveryModule ?? true;

        if (table.IsDeliveryChannel)
        {
            if (!includesDelivery)
            {
                throw new KeyNotFoundException("Table not found.");
            }

            return;
        }

        if (!includesTables)
        {
            throw new KeyNotFoundException("Table not found.");
        }
    }

    private static void EnsurePublicOrderingOpen(Company company)
    {
        var status = BuildPublicOrderingStatus(company);
        if (!status.IsOpen)
        {
            throw new InvalidOperationException(status.Message);
        }
    }

    private static PublicOrderingStatus BuildPublicOrderingStatus(Company company)
    {
        var timeZone = GetBusinessTimeZone();
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        var displayName = string.IsNullOrWhiteSpace(company.TradeName)
            ? company.LegalName
            : company.TradeName;

        if (!IsPublicServiceDayAllowed(company, localNow.DayOfWeek))
        {
            var daysLabel = BuildPublicServiceDaysLabel(ParsePublicServiceDays(company.AiAssistantServiceDays));
            var closedDayMessage =
                $"No momento {displayName} nao recebe pedidos neste dia. O sistema de pedidos fica fechado fora dos dias de funcionamento. Dias de atendimento: {daysLabel}.";

            return new PublicOrderingStatus(false, closedDayMessage);
        }

        if (!TryResolvePublicOrderingWindow(company, out var startTime, out var endTime))
        {
            return new PublicOrderingStatus(true, null);
        }

        var localTime = TimeOnly.FromDateTime(localNow);
        var isOpen = IsWithinServiceWindow(localTime, startTime, endTime);
        if (isOpen)
        {
            return new PublicOrderingStatus(true, null);
        }

        var message =
            $"No momento {displayName} esta fora do horario de atendimento. O sistema de pedidos fica fechado fora desse horario. Atendimento: {startTime:HH\\:mm} as {endTime:HH\\:mm}.";

        return new PublicOrderingStatus(false, message);
    }

    private static bool TryResolvePublicOrderingWindow(Company company, out TimeOnly startTime, out TimeOnly endTime)
    {
        startTime = default;
        endTime = default;

        if (string.IsNullOrWhiteSpace(company.AiAssistantServiceStartTime) ||
            string.IsNullOrWhiteSpace(company.AiAssistantServiceEndTime))
        {
            return false;
        }

        return TimeOnly.TryParseExact(company.AiAssistantServiceStartTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out startTime) &&
               TimeOnly.TryParseExact(company.AiAssistantServiceEndTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out endTime);
    }

    private static List<int>? ParsePublicServiceDays(string? serviceDays)
    {
        if (string.IsNullOrWhiteSpace(serviceDays))
        {
            return null;
        }

        var days = serviceDays
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => int.TryParse(item, NumberStyles.None, CultureInfo.InvariantCulture, out var day) ? day : -1)
            .Where(item => item is >= 0 and <= 6)
            .Distinct()
            .OrderBy(item => item)
            .ToList();

        return days.Count is 0 or 7 ? null : days;
    }

    private static bool IsPublicServiceDayAllowed(Company company, DayOfWeek dayOfWeek)
    {
        var serviceDays = ParsePublicServiceDays(company.AiAssistantServiceDays);
        return serviceDays is null || serviceDays.Contains((int)dayOfWeek);
    }

    private static string BuildPublicServiceDaysLabel(IReadOnlyCollection<int>? serviceDays)
    {
        if (serviceDays is null || serviceDays.Count == 0 || serviceDays.Count == 7)
        {
            return "todos os dias";
        }

        return string.Join(", ", OrderedPublicServiceDays()
            .Where(item => serviceDays.Contains(item.Value))
            .Select(item => item.Label));
    }

    private static IEnumerable<(int Value, string Label)> OrderedPublicServiceDays()
    {
        yield return (1, "segunda");
        yield return (2, "terca");
        yield return (3, "quarta");
        yield return (4, "quinta");
        yield return (5, "sexta");
        yield return (6, "sabado");
        yield return (0, "domingo");
    }

    private static bool IsWithinServiceWindow(TimeOnly currentTime, TimeOnly startTime, TimeOnly endTime)
    {
        return startTime == endTime ||
               (startTime < endTime
                   ? currentTime >= startTime && currentTime <= endTime
                   : currentTime >= startTime || currentTime <= endTime);
    }

    private static void EnsurePublicWaiterCallEnabled(DiningTable table, Subscription? activeSubscription)
    {
        EnsurePublicOrderingEnabled(table, activeSubscription);

        if (!(activeSubscription?.IncludesWaiterCallModule ?? true))
        {
            throw new KeyNotFoundException("Table not found.");
        }
    }

    private async Task RecalculateTableStatusAsync(DiningTable table, CancellationToken cancellationToken)
    {
        if (table.IsDeliveryChannel)
        {
            table.ChangeStatus(TableStatus.Available);
            return;
        }

        var hasOpenOrders = await _context.CustomerOrders.AnyAsync(
            item => item.DiningTableId == table.Id &&
                    item.IsActive &&
                    item.Status != OrderStatus.Delivered &&
                    item.Status != OrderStatus.Cancelled,
            cancellationToken);

        table.ChangeStatus(hasOpenOrders ? TableStatus.Occupied : TableStatus.Available);
    }

    private async Task<string> GenerateUniqueTableCodeAsync(Guid companyId, CancellationToken cancellationToken)
    {
        return await GenerateUniqueTableCodeAsync(companyId, null, cancellationToken);
    }

    private async Task<string> GenerateUniqueTableCodeAsync(Guid companyId, string? preferredCode, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(preferredCode))
        {
            var normalizedPreferredCode = preferredCode.Trim().ToUpperInvariant();

            if (!await _context.DiningTables.AnyAsync(
                    item => item.CompanyId == companyId && item.InternalCode == normalizedPreferredCode,
                    cancellationToken))
            {
                return normalizedPreferredCode;
            }

            var suffix = 2;
            var candidateCode = $"{normalizedPreferredCode}-{suffix}";

            while (await _context.DiningTables.AnyAsync(
                       item => item.CompanyId == companyId && item.InternalCode == candidateCode,
                       cancellationToken))
            {
                suffix++;
                candidateCode = $"{normalizedPreferredCode}-{suffix}";
            }

            return candidateCode;
        }

        var sequence = await _context.DiningTables.CountAsync(item => item.CompanyId == companyId, cancellationToken) + 1;
        var code = $"M-{sequence:000}";

        while (await _context.DiningTables.AnyAsync(
                   item => item.CompanyId == companyId && item.InternalCode == code,
                   cancellationToken))
        {
            sequence++;
            code = $"M-{sequence:000}";
        }

        return code;
    }

    private async Task<string> GenerateUniquePublicCodeAsync(CancellationToken cancellationToken)
    {
        string publicCode;

        do
        {
            publicCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();
        }
        while (await _context.QrCodeAccesses.AnyAsync(item => item.PublicCode == publicCode, cancellationToken));

        return publicCode;
    }

    private async Task<List<MenuCategoryDto>> BuildMenuAsync(Guid companyId, bool includeInactiveItems, CancellationToken cancellationToken)
    {
        var categories = await _context.MenuCategories
            .AsNoTracking()
            .AsSplitQuery()
            .Where(item => item.CompanyId == companyId && item.IsActive)
            .Include(item => item.Items)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

        var menuItemIds = categories
            .SelectMany(category => category.Items)
            .Where(item => includeInactiveItems || item.IsActive)
            .Select(item => item.Id)
            .ToList();

        var menuItemIdsWithAdditionalOptions = menuItemIds.Count == 0
            ? new List<Guid>()
            : await _context.MenuItemAdditionalGroups
                .AsNoTracking()
                .Where(group =>
                    menuItemIds.Contains(group.MenuItemId) &&
                    group.IsActive &&
                    group.MaxAdditionalSelections != 0 &&
                    group.Options.Any(option => option.IsActive))
                .Select(group => group.MenuItemId)
                .Distinct()
                .ToListAsync(cancellationToken);

        var itemIdLookup = menuItemIdsWithAdditionalOptions.ToHashSet();
        var startingPriceLookup = await BuildMenuItemStartingPriceLookupAsync(menuItemIds, cancellationToken);

        return categories.Select(item => new MenuCategoryDto
        {
            Id = item.Id,
            Name = item.Name,
            ImageUrl = NormalizeMenuImagePath(item.ImageUrl),
            DisplayOrder = item.DisplayOrder,
            Items = item.Items
                .Where(menuItem => includeInactiveItems || menuItem.IsActive)
                .OrderBy(menuItem => menuItem.DisplayOrder)
                .ThenBy(menuItem => menuItem.Name)
                .Select(menuItem => MapPublicMenuItem(
                    menuItem,
                    itemIdLookup.Contains(menuItem.Id),
                    startingPriceLookup.GetValueOrDefault(menuItem.Id, menuItem.Price)))
                .ToList()
        }).ToList();
    }

    private async Task<List<MenuCategoryDto>> BuildPublicMenuAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var categories = await _context.MenuCategories
            .AsNoTracking()
            .AsSplitQuery()
            .Where(item => item.CompanyId == companyId && item.IsActive)
            .Include(item => item.Items.Where(menuItem => menuItem.IsActive))
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

        var menuItemIds = categories
            .SelectMany(category => category.Items)
            .Select(item => item.Id)
            .ToList();

        var menuItemIdsWithAdditionalOptions = menuItemIds.Count == 0
            ? new List<Guid>()
            : await _context.MenuItemAdditionalGroups
                .AsNoTracking()
                .Where(group =>
                    menuItemIds.Contains(group.MenuItemId) &&
                    group.IsActive &&
                    group.MaxAdditionalSelections != 0 &&
                    group.Options.Any(option => option.IsActive))
                .Select(group => group.MenuItemId)
                .Distinct()
                .ToListAsync(cancellationToken);

        var itemIdLookup = menuItemIdsWithAdditionalOptions.ToHashSet();
        var startingPriceLookup = await BuildMenuItemStartingPriceLookupAsync(menuItemIds, cancellationToken);

        return categories.Select(category => new MenuCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            ImageUrl = NormalizeMenuImagePath(category.ImageUrl),
            DisplayOrder = category.DisplayOrder,
            Items = category.Items
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Name)
                .Select(item => MapPublicMenuItem(
                    item,
                    itemIdLookup.Contains(item.Id),
                    startingPriceLookup.GetValueOrDefault(item.Id, item.Price)))
                .ToList()
        }).ToList();
    }

    private async Task<Dictionary<Guid, decimal>> BuildMenuItemStartingPriceLookupAsync(
        IReadOnlyCollection<Guid> menuItemIds,
        CancellationToken cancellationToken)
    {
        if (menuItemIds.Count == 0)
        {
            return [];
        }

        var minimumPositiveAdditionalPrices = await _context.MenuItemAdditionalOptions
            .AsNoTracking()
            .Where(option =>
                menuItemIds.Contains(option.MenuItemId) &&
                option.IsActive &&
                option.Price > 0 &&
                option.MenuItemAdditionalGroup.IsActive &&
                option.MenuItemAdditionalGroup.MaxAdditionalSelections != 0)
            .GroupBy(option => option.MenuItemId)
            .Select(group => new { MenuItemId = group.Key, Price = group.Min(item => item.Price) })
            .ToDictionaryAsync(item => item.MenuItemId, item => item.Price, cancellationToken);

        var menuItemPrices = await _context.MenuItems
            .AsNoTracking()
            .Where(item => menuItemIds.Contains(item.Id))
            .Select(item => new { item.Id, item.Price })
            .ToListAsync(cancellationToken);

        return menuItemPrices.ToDictionary(
            item => item.Id,
            item => item.Price > 0
                ? item.Price
                : minimumPositiveAdditionalPrices.GetValueOrDefault(item.Id, item.Price));
    }

    private async Task<List<OrderItem>> BuildOrderItemsAsync(
        Guid companyId,
        Guid tenantId,
        List<OrderItemInputDto> items,
        List<MenuOrderSelectionDto> menuSelections,
        CancellationToken cancellationToken)
    {
        if (menuSelections.Count != 0)
        {
            var menuItemIds = menuSelections
                .Where(item => item.MenuItemId != Guid.Empty && item.Quantity > 0)
                .Select(item => item.MenuItemId)
                .Distinct()
                .ToList();

            var menuItems = await _context.MenuItems
                .Include(item => item.MenuCategory)
                .Include(item => item.AdditionalGroups)
                    .ThenInclude(item => item.Options)
                .Where(item => item.CompanyId == companyId && item.IsActive && menuItemIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, cancellationToken);

            return menuSelections
    .Where(item => item.Quantity > 0 && menuItems.ContainsKey(item.MenuItemId))
    .Select(item =>
    {
        var menuItem = menuItems[item.MenuItemId];

        ValidateAdditionalSelections(menuItem, item.AdditionalOptionIds);

        var selectedAdditionalSelections = BuildOrderItemAdditionalSelections(
            menuItem,
            item.AdditionalOptionIds,
            tenantId);

        return new OrderItem(
            tenantId,
            menuItem.Name,
            item.Quantity,
            menuItem.Price + selectedAdditionalSelections.Sum(selection => selection.UnitPrice),
            item.MenuItemId,
            menuItem.MenuCategory.Name,
            NormalizeMenuImagePath(menuItem.ImageUrl) ?? NormalizeMenuImagePath(menuItem.MenuCategory.ImageUrl),
            item.Notes,
            menuItem.Price,
            selectedAdditionalSelections);
    })
    .ToList();
        }

        return items
            .Where(item => !string.IsNullOrWhiteSpace(item.Name) && item.Quantity > 0)
            .Select(item =>
                new OrderItem(
                    tenantId,
                    item.Name,
                    item.Quantity,
                    item.UnitPrice,
                    null,
                    null,
                    null,
                    item.Notes))
            .ToList();
    }

    private static void ValidateAdditionalSelections(MenuItem menuItem, IReadOnlyCollection<Guid> selectedOptionIds)
    {
        ArgumentNullException.ThrowIfNull(menuItem);
        ArgumentNullException.ThrowIfNull(selectedOptionIds);

        var distinctSelectedOptionIds = selectedOptionIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (distinctSelectedOptionIds.Count == 0)
        {
            if (menuItem.Price <= 0 &&
                menuItem.AdditionalGroups
                    .Where(group => group.IsActive && group.MaxAdditionalSelections != 0)
                    .SelectMany(group => group.Options)
                    .Any(option => option.IsActive && option.Price > 0))
            {
                throw new InvalidOperationException(
                    $"Escolha uma variacao para o item '{menuItem.Name}'.");
            }

            return;
        }

        var activeGroups = menuItem.AdditionalGroups
            .Where(group => group.IsActive)
            .ToList();

        var activeOptions = activeGroups
            .SelectMany(group => group.Options.Where(option => option.IsActive))
            .ToList();

        var validOptionIds = activeOptions
            .Select(option => option.Id)
            .ToHashSet();

        var hasInvalidOption = distinctSelectedOptionIds
            .Any(optionId => !validOptionIds.Contains(optionId));

        if (hasInvalidOption)
        {
            throw new InvalidOperationException(
                $"O item '{menuItem.Name}' recebeu adicionais inválidos.");
        }

        if (menuItem.Price <= 0 &&
            activeOptions.Any(option => option.Price > 0) &&
            !activeOptions.Any(option => option.Price > 0 && distinctSelectedOptionIds.Contains(option.Id)))
        {
            throw new InvalidOperationException(
                $"Escolha uma variacao para o item '{menuItem.Name}'.");
        }

        if (menuItem.MaxAdditionalSelections.HasValue &&
            distinctSelectedOptionIds.Count > menuItem.MaxAdditionalSelections.Value)
        {
            throw new InvalidOperationException(
                $"O item '{menuItem.Name}' permite no máximo {menuItem.MaxAdditionalSelections.Value} adicional(is).");
        }

        foreach (var group in activeGroups)
        {
            if (!group.MaxAdditionalSelections.HasValue)
            {
                continue;
            }

            var selectedFromGroupCount = group.Options
                .Where(option => option.IsActive)
                .Count(option => distinctSelectedOptionIds.Contains(option.Id));

            if (selectedFromGroupCount > group.MaxAdditionalSelections.Value)
            {
                throw new InvalidOperationException(
                    $"O grupo '{group.Name}' permite no máximo {group.MaxAdditionalSelections.Value} adicional(is).");
            }
        }
    }

    private async Task<CustomerOrder> PersistOrderAsync(
        DiningTable table,
        string? customerName,
        string? notes,
        string? deliveryPhone,
        string? deliveryAddress,
        string? deliveryNumber,
        string? deliveryComplement,
        string? deliveryPostalCode,
        string fulfillmentType,
        List<OrderItem> orderItems,
        PaymentMethod paymentMethod,
        string? couponCode,
        CancellationToken cancellationToken,
        Guid? salesAgentId = null)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(
                item => item.Id == table.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Company not found.");

        var isPickupOrder = table.IsDeliveryChannel && fulfillmentType == FulfillmentTypePickup;
        var normalizedPostalCode = table.IsDeliveryChannel && !isPickupOrder
            ? NormalizePostalCodeOrThrow(deliveryPostalCode, nameof(deliveryPostalCode))
            : null;
        var itemsSubtotal = orderItems.Sum(item => item.TotalPrice);
        var freightQuote = table.IsDeliveryChannel && !isPickupOrder
            ? await _deliveryFreightService.QuoteAsync(company, normalizedPostalCode, itemsSubtotal, cancellationToken)
            : null;
        var freightAmount = freightQuote?.IsAvailable == true ? freightQuote.FreightAmount : 0m;
        var freightDistanceKm = freightQuote?.IsAvailable == true ? freightQuote.DistanceKm : null;
        var freightProvider = freightQuote?.IsAvailable == true ? freightQuote.Provider : null;
        var nextNumber = company.ReserveNextOrderNumber();
        var storedDeliveryAddress = isPickupOrder ? PickupAddressMarker : deliveryAddress;
        var storedDeliveryNumber = isPickupOrder ? null : deliveryNumber;
        var storedDeliveryComplement = isPickupOrder ? null : deliveryComplement;
        var order = new CustomerOrder(
            table.TenantId,
            table.CompanyId,
            table.Id,
            nextNumber,
            customerName,
            notes,
            deliveryPhone,
            storedDeliveryAddress,
            storedDeliveryNumber,
            storedDeliveryComplement,
            normalizedPostalCode,
            freightAmount,
            freightDistanceKm,
            freightProvider,
            orderItems,
            paymentMethod);

        await _couponService.ApplyCouponToOrderAsync(
            table.CompanyId,
            order,
            couponCode,
            itemsSubtotal,
            incrementUsage: true,
            cancellationToken);

        if (salesAgentId.HasValue)
        {
            order.SetSalesAgent(salesAgentId.Value, Domain.Enums.SalesOrigin.SellerLink);
        }

        if (!company.EnableAutomaticPrinting)
        {
            order.DisablePrinting();
        }

        table.ChangeStatus(TableStatus.Occupied);

        await _context.CustomerOrders.AddAsync(order, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return order;
    }

    private async Task TryNotifyDeliveryOrderAsync(Guid orderId, bool isUpdate, CancellationToken cancellationToken)
    {
        try
        {
            await _whatsAppIntegrationService.TrySendDeliveryOrderConfirmationAsync(orderId, isUpdate, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Falha ao enviar confirmacao de delivery por WhatsApp para o pedido {OrderId}.",
                orderId);
        }
    }

    private async Task UpsertDeliveryCustomerProfileAsync(
        CustomerOrder order,
        string? deliveryNeighborhood,
        CancellationToken cancellationToken)
    {
        if (!order.DiningTable.IsDeliveryChannel || string.IsNullOrWhiteSpace(order.DeliveryPhone))
        {
            return;
        }

        var normalizedPhone = DeliveryCustomerProfile.NormalizePhone(order.DeliveryPhone);
        var profile = await _context.DeliveryCustomerProfiles
            .FirstOrDefaultAsync(
                item => item.CompanyId == order.CompanyId &&
                        item.Phone == normalizedPhone,
                cancellationToken);
        var isPickupOrder = IsPickupOrder(order);

        if (profile is null)
        {
            profile = new DeliveryCustomerProfile(
                order.TenantId,
                order.CompanyId,
                normalizedPhone,
                order.CustomerName,
                isPickupOrder ? null : order.DeliveryAddress,
                isPickupOrder ? null : order.DeliveryNumber,
                isPickupOrder ? null : deliveryNeighborhood,
                isPickupOrder ? null : order.DeliveryComplement,
                isPickupOrder ? null : order.DeliveryPostalCode,
                order.SubmittedAtUtc);

            await _context.DeliveryCustomerProfiles.AddAsync(profile, cancellationToken);
        }
        else
        {
            profile.Activate();
            profile.Update(
                order.CustomerName,
                isPickupOrder ? profile.DeliveryAddress : order.DeliveryAddress,
                isPickupOrder ? profile.DeliveryNumber : order.DeliveryNumber,
                isPickupOrder ? profile.DeliveryNeighborhood : deliveryNeighborhood,
                isPickupOrder ? profile.DeliveryComplement : order.DeliveryComplement,
                isPickupOrder ? profile.DeliveryPostalCode : order.DeliveryPostalCode,
                order.SubmittedAtUtc);
        }

        await AddCustomerOrderHistoryAsync(profile, order, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task AddCustomerOrderHistoryAsync(
        DeliveryCustomerProfile profile,
        CustomerOrder order,
        CancellationToken cancellationToken)
    {
        var alreadyExists = await _context.CustomerOrderHistories
            .AnyAsync(item => item.OrderId == order.Id, cancellationToken);
        if (alreadyExists)
        {
            return;
        }

        var items = order.Items
            .Select(item => new CustomerOrderHistoryItem(order.TenantId, item.Name, item.Quantity))
            .ToList();

        if (items.Count == 0)
        {
            return;
        }

        await _context.CustomerOrderHistories.AddAsync(
            new CustomerOrderHistory(
                order.TenantId,
                order.CompanyId,
                profile.Id,
                order.Id,
                order.TotalAmount,
                order.SubmittedAtUtc,
                items),
            cancellationToken);
    }

    private async Task ValidateOwnerPasswordAsync(
        WorkspaceSessionContext session,
        string password,
        CancellationToken cancellationToken)
    {
        var companySecurity = await _context.Companies
            .AsNoTracking()
            .Where(item => item.Id == session.CompanyId && item.IsActive)
            .Select(item => new
            {
                item.AdminMasterPasswordHash,
                OwnerPasswordHash = item.Users
                    .Where(user => user.Role == UserRole.Owner && user.IsActive)
                    .Select(user => user.PasswordHash)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");

        var ownerPasswordMatches = !string.IsNullOrWhiteSpace(companySecurity.OwnerPasswordHash) &&
            _passwordHasher.Verify(password, companySecurity.OwnerPasswordHash);
        var masterPasswordMatches = !string.IsNullOrWhiteSpace(companySecurity.AdminMasterPasswordHash) &&
            _passwordHasher.Verify(password, companySecurity.AdminMasterPasswordHash);

        if (!ownerPasswordMatches && !masterPasswordMatches)
        {
            throw new InvalidOperationException("Senha protegida incorreta.");
        }
    }

    private static void EnsureOrderCanBeEdited(CustomerOrder order)
    {
        if (order.Status is not (OrderStatus.Pending or OrderStatus.InKitchen or OrderStatus.Ready))
        {
            throw new InvalidOperationException("Apenas pedidos pendentes, em preparo ou prontos podem ser editados.");
        }
    }

    private static List<OrderItem> BuildEditedOrderItems(
        WorkspaceSessionContext session,
        CustomerOrder order,
        List<UpdateCustomerOrderItemRequestDto> requestItems)
    {
        ArgumentNullException.ThrowIfNull(requestItems);

        var currentItems = order.Items.ToDictionary(item => item.Id);
        var updatedItems = new List<OrderItem>();

        foreach (var requestItem in requestItems)
        {
            if (requestItem.Quantity <= 0)
            {
                continue;
            }

            if (requestItem.Id.HasValue && currentItems.TryGetValue(requestItem.Id.Value, out var currentItem))
            {
                updatedItems.Add(CloneOrderItemForEdit(session.TenantId, currentItem, requestItem.Quantity, requestItem.Notes));
                continue;
            }

            if (string.IsNullOrWhiteSpace(requestItem.Name))
            {
                continue;
            }

            updatedItems.Add(new OrderItem(
                session.TenantId,
                requestItem.Name,
                requestItem.Quantity,
                requestItem.UnitPrice ?? 0m,
                notes: requestItem.Notes));
        }

        if (updatedItems.Count == 0)
        {
            throw new ArgumentException("O pedido precisa manter pelo menos um item.");
        }

        return updatedItems;
    }

    private static OrderItem CloneOrderItemForEdit(Guid tenantId, OrderItem currentItem, decimal quantity, string? notes)
    {
        var selections = currentItem.AdditionalSelections.Select(selection => new OrderItemAdditionalSelection(
            tenantId,
            selection.GroupName,
            selection.OptionName,
            selection.UnitPrice,
            selection.SourceMenuItemAdditionalOptionId));

        return new OrderItem(
            tenantId,
            currentItem.Name,
            quantity,
            currentItem.BaseUnitPrice,
            currentItem.SourceMenuItemId,
            currentItem.CategoryName,
            currentItem.ImageUrl,
            notes,
            currentItem.BaseUnitPrice,
            selections);
    }

    private async Task ApplyOrderStatusAsync(CustomerOrder order, OrderStatus nextStatus, CancellationToken cancellationToken)
    {
        switch (nextStatus)
        {
            case OrderStatus.Pending:
                break;
            case OrderStatus.InKitchen:
                if (order.Status == OrderStatus.Ready)
                {
                    order.ReturnToPreparation();
                }
                else
                {
                    order.MoveToKitchen();
                }

                order.DiningTable.ChangeStatus(TableStatus.Occupied);
                break;

            case OrderStatus.Ready:
                if (order.Status == OrderStatus.Delivered)
                {
                    throw new InvalidOperationException("Pedidos finalizados nao voltam para a cozinha.");
                }

                order.MarkReady();
                order.DiningTable.ChangeStatus(TableStatus.Occupied);
                break;

            case OrderStatus.Delivered:
                order.MarkDelivered();
                await RecalculateTableStatusAsync(order.DiningTable, cancellationToken);
                break;

            case OrderStatus.Cancelled:
                order.Cancel();
                await RecalculateTableStatusAsync(order.DiningTable, cancellationToken);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(nextStatus), "Unsupported order status.");
        }
    }

    private async Task DeleteOrdersAsync(
        WorkspaceSessionContext session,
        IEnumerable<CustomerOrder> orders,
        string deletionReason,
        CancellationToken cancellationToken)
    {
        var materializedOrders = orders.ToList();

        if (materializedOrders.Count == 0)
        {
            return;
        }

        var deletedAtUtc = DateTime.UtcNow;
        var distinctTables = materializedOrders
            .Select(item => item.DiningTable)
            .DistinctBy(item => item.Id)
            .ToList();

        foreach (var order in materializedOrders)
        {
            await _context.DeletedOrderRecords.AddAsync(
                BuildDeletedOrderRecord(session, order, deletedAtUtc, deletionReason),
                cancellationToken);

            _context.OrderItems.RemoveRange(order.Items);
            _context.CustomerOrders.Remove(order);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "SecurityAudit action={Action} company={CompanyId} user={UserId} affected={AffectedCount} reason={Reason}",
            "delete-orders",
            session.CompanyId,
            session.UserId,
            materializedOrders.Count,
            deletionReason);

        foreach (var table in distinctTables)
        {
            await RecalculateTableStatusAsync(table, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ArchiveCurrentFlowOrdersAsync(
        WorkspaceSessionContext session,
        IEnumerable<CustomerOrder> orders,
        string deletionReason,
        CancellationToken cancellationToken)
    {
        var materializedOrders = orders.ToList();

        if (materializedOrders.Count == 0)
        {
            return;
        }

        var deletedAtUtc = DateTime.UtcNow;
        var distinctTables = materializedOrders
            .Select(item => item.DiningTable)
            .DistinctBy(item => item.Id)
            .ToList();

        foreach (var order in materializedOrders)
        {
            await _context.DeletedOrderRecords.AddAsync(
                BuildDeletedOrderRecord(session, order, deletedAtUtc, deletionReason),
                cancellationToken);

            order.DisablePrinting();
            order.Deactivate();
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "SecurityAudit action={Action} company={CompanyId} user={UserId} affected={AffectedCount} reason={Reason}",
            "archive-current-order-flow",
            session.CompanyId,
            session.UserId,
            materializedOrders.Count,
            deletionReason);

        foreach (var table in distinctTables)
        {
            await RecalculateTableStatusAsync(table, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private DailyCashReportData BuildDailyCashReportData(
        WorkspaceSessionContext session,
        IReadOnlyList<CustomerOrder> activeOrders,
        IReadOnlyList<DeletedOrderRecord> deletedOrders,
        TimeZoneInfo timeZone,
        DateTime dayStartUtc,
        DateTime dayEndUtc,
        DateTime localNow)
    {
        var submittedTodayOrders = activeOrders
            .Where(item => IsWithinBusinessDay(item.SubmittedAtUtc, dayStartUtc, dayEndUtc))
            .OrderBy(item => item.SubmittedAtUtc)
            .ThenBy(item => item.Number)
            .ToList();

        var activeNonCancelledOrders = activeOrders
            .Where(item => item.Status != OrderStatus.Cancelled)
            .OrderBy(item => item.SubmittedAtUtc)
            .ThenBy(item => item.Number)
            .ToList();

        var pendingOrders = activeNonCancelledOrders
            .Where(item => item.PaymentStatus != PaymentStatus.Paid)
            .ToList();

        var paidOrders = activeNonCancelledOrders
            .Where(item => item.PaymentStatus == PaymentStatus.Paid && IsWithinBusinessDay(item.PaidAtUtc, dayStartUtc, dayEndUtc))
            .OrderBy(item => item.PaidAtUtc ?? item.SubmittedAtUtc)
            .ThenBy(item => item.Number)
            .ToList();

        var deletedPaidOrders = deletedOrders
            .Where(item => item.PaymentStatus == PaymentStatus.Paid)
            .ToList();

        var totalPaidValue = paidOrders.Sum(item => item.TotalAmount) + deletedPaidOrders.Sum(item => item.TotalAmount);
        var totalPendingValue = pendingOrders.Sum(item => item.TotalAmount);

        var paymentSummaries = Enum.GetValues<PaymentMethod>()
            .Where(item => item != PaymentMethod.Undefined)
            .Select(paymentMethod =>
            {
                var activeCount = paidOrders.Count(item => item.PaymentMethod == paymentMethod);
                var deletedCount = deletedPaidOrders.Count(item => item.PaymentMethod == paymentMethod);
                var totalCount = activeCount + deletedCount;
                var totalValue = paidOrders.Where(item => item.PaymentMethod == paymentMethod).Sum(item => item.TotalAmount)
                    + deletedPaidOrders.Where(item => item.PaymentMethod == paymentMethod).Sum(item => item.TotalAmount);

                return new DailyCashReportPaymentSummary
                {
                    Label = FormatPaymentMethod(paymentMethod),
                    CountLabel = $"{totalCount} pedido(s)",
                    TotalLabel = FormatCurrencyValue(totalValue)
                };
            })
            .ToList();

        var paymentDifferences = activeOrders
            .Where(item =>
                HasPaymentDifference(item.RequestedPaymentMethod, item.PaymentMethod) &&
                (item.PaymentStatus != PaymentStatus.Paid || IsWithinBusinessDay(item.PaidAtUtc, dayStartUtc, dayEndUtc)))
            .Select(item => new DailyCashReportPaymentDifferenceRow
            {
                OrderLabel = $"Pedido #{item.Number}",
                TableLabel = item.DiningTable.Name,
                RequestedPaymentLabel = FormatPaymentMethod(item.RequestedPaymentMethod),
                AppliedPaymentLabel = FormatPaymentMethod(item.PaymentMethod),
                TotalLabel = FormatCurrencyValue(item.TotalAmount),
                ContextLabel = item.PaymentStatus == PaymentStatus.Paid
                    ? $"Pago em {FormatBusinessDateTime(item.PaidAtUtc ?? item.SubmittedAtUtc, timeZone)}"
                    : "Pedido ainda ativo no caixa"
            })
            .Concat(
                deletedOrders
                    .Where(item => HasPaymentDifference(item.RequestedPaymentMethod, item.PaymentMethod))
                    .Select(item => new DailyCashReportPaymentDifferenceRow
                    {
                        OrderLabel = $"Pedido #{item.OrderNumber}",
                        TableLabel = item.TableName,
                        RequestedPaymentLabel = FormatPaymentMethod(item.RequestedPaymentMethod),
                        AppliedPaymentLabel = FormatPaymentMethod(item.PaymentMethod),
                        TotalLabel = FormatCurrencyValue(item.TotalAmount),
                        ContextLabel = $"{item.DeletionReason} por {item.DeletedByUserName}"
                    }))
            .OrderBy(item => item.OrderLabel)
            .ToList();

        var cancelledTodayCount =
            submittedTodayOrders.Count(item => item.Status == OrderStatus.Cancelled) +
            deletedOrders.Count(item => item.Status == OrderStatus.Cancelled);

        return new DailyCashReportData
        {
            RestaurantName = session.RestaurantName,
            ReportDateLabel = localNow.ToString("dd/MM/yyyy", PtBrCulture),
            GeneratedAtLabel = localNow.ToString("dd/MM/yyyy HH:mm", PtBrCulture),
            Metrics =
            [
                new DailyCashReportMetric
                {
                    Label = "Pedidos lancados hoje",
                    Value = submittedTodayOrders.Count.ToString(PtBrCulture),
                    Detail = "Pedidos criados dentro da data de referencia"
                },
                new DailyCashReportMetric
                {
                    Label = "Pagos hoje",
                    Value = (paidOrders.Count + deletedPaidOrders.Count).ToString(PtBrCulture),
                    Detail = FormatCurrencyValue(totalPaidValue)
                },
                new DailyCashReportMetric
                {
                    Label = "A cobrar hoje",
                    Value = pendingOrders.Count.ToString(PtBrCulture),
                    Detail = FormatCurrencyValue(totalPendingValue)
                },
                new DailyCashReportMetric
                {
                    Label = "Cancelados hoje",
                    Value = cancelledTodayCount.ToString(PtBrCulture),
                    Detail = "Inclui cancelamentos ainda ativos e os ja apagados"
                },
                new DailyCashReportMetric
                {
                    Label = "Apagados hoje",
                    Value = deletedOrders.Count.ToString(PtBrCulture),
                    Detail = "Historico preservado para auditoria"
                },
                new DailyCashReportMetric
                {
                    Label = "Divergencias",
                    Value = paymentDifferences.Count.ToString(PtBrCulture),
                    Detail = "Forma solicitada x forma aplicada"
                }
            ],
            PaymentSummaries = paymentSummaries,
            PendingOrders = pendingOrders.Select(item => new DailyCashReportOrderRow
            {
                OrderLabel = $"Pedido #{item.Number}",
                TableLabel = item.DiningTable.Name,
                StatusLabel = FormatOrderStatus(item.Status),
                PaymentLabel = $"{FormatPaymentMethod(item.PaymentMethod)} â€¢ {FormatPaymentStatus(item.PaymentStatus)}",
                TotalLabel = FormatCurrencyValue(item.TotalAmount),
                TimeLabel = FormatBusinessDateTime(item.SubmittedAtUtc, timeZone),
                ItemsLabel = BuildItemsSummary(item.Items.Select(orderItem => (orderItem.Quantity, BuildOrderItemSummaryLabel(orderItem)))),
                NotesLabel = BuildActiveOrderNotes(item, timeZone)
            }).ToList(),
            PaidOrders = paidOrders.Select(item => new DailyCashReportOrderRow
            {
                OrderLabel = $"Pedido #{item.Number}",
                TableLabel = item.DiningTable.Name,
                StatusLabel = FormatOrderStatus(item.Status),
                PaymentLabel = $"{FormatPaymentMethod(item.PaymentMethod)} â€¢ {FormatPaymentStatus(item.PaymentStatus)}",
                TotalLabel = FormatCurrencyValue(item.TotalAmount),
                TimeLabel = FormatBusinessDateTime(item.PaidAtUtc ?? item.SubmittedAtUtc, timeZone),
                ItemsLabel = BuildItemsSummary(item.Items.Select(orderItem => (orderItem.Quantity, BuildOrderItemSummaryLabel(orderItem)))),
                NotesLabel = BuildActiveOrderNotes(item, timeZone)
            }).ToList(),
            PaymentDifferences = paymentDifferences,
            DeletedOrders = deletedOrders.Select(item => new DailyCashReportDeletedOrderRow
            {
                OrderLabel = $"Pedido #{item.OrderNumber}",
                TableLabel = item.TableName,
                PaymentLabel = $"{FormatPaymentMethod(item.PaymentMethod)} â€¢ {FormatPaymentStatus(item.PaymentStatus)}",
                TotalLabel = FormatCurrencyValue(item.TotalAmount),
                DeletedAtLabel = FormatBusinessDateTime(item.DeletedAtUtc, timeZone),
                ReasonLabel = $"{item.DeletionReason} por {item.DeletedByUserName}",
                ItemsLabel = item.ItemsSummary
            }).ToList()
        };
    }

    private DeletedOrderRecord BuildDeletedOrderRecord(
        WorkspaceSessionContext session,
        CustomerOrder order,
        DateTime deletedAtUtc,
        string deletionReason)
    {
        return new DeletedOrderRecord(
            session.TenantId,
            session.CompanyId,
            order.Id,
            order.Number,
            order.DiningTable.Name,
            order.CustomerName,
            BuildOperationalOrderNotes(order),
            BuildItemsSummary(order.Items.Select(item => (item.Quantity, BuildOrderItemSummaryLabel(item)))),
            order.Status,
            order.PaymentMethod,
            order.RequestedPaymentMethod,
            order.PaymentStatus,
            order.PrintStatus,
            order.TotalAmount,
            order.SubmittedAtUtc,
            order.PaidAtUtc,
            order.PrintedAtUtc,
            deletedAtUtc,
            session.UserId,
            string.IsNullOrWhiteSpace(session.FullName) ? session.Email : session.FullName,
            deletionReason);
    }

    private static string BuildItemsSummary(IEnumerable<(decimal Quantity, string Name)> items)
    {
        var normalizedItems = items
            .Select(item =>
            {
                var itemName = string.IsNullOrWhiteSpace(item.Name)
                    ? "Item sem nome"
                    : item.Name.Trim();

                return $"{item.Quantity.ToString("0.##", PtBrCulture)}x {itemName}".Trim();
            })
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();

        if (normalizedItems.Count == 0)
        {
            return "Sem itens registrados.";
        }

        var summary = string.Join(" | ", normalizedItems);

        if (summary.Length <= 3900)
        {
            return summary;
        }

        return $"{summary[..3897]}...";
    }

    private static string BuildOrderItemSummaryLabel(OrderItem item)
    {
        if (item.AdditionalSelections.Count == 0)
        {
            return item.Name;
        }

        var additions = string.Join(
            ", ",
            item.AdditionalSelections.Select(selection => $"{selection.GroupName}: {selection.OptionName}"));

        return $"{item.Name} [{additions}]";
    }

    private static string? BuildActiveOrderNotes(CustomerOrder order, TimeZoneInfo timeZone)
    {
        var notes = new List<string>();

        AppendOperationalNotes(notes, order);

        if (order.PaidAtUtc.HasValue)
        {
            notes.Add($"Pago em {FormatBusinessDateTime(order.PaidAtUtc.Value, timeZone)}");
        }

        if (order.PrintedAtUtc.HasValue)
        {
            notes.Add($"Impresso em {FormatBusinessDateTime(order.PrintedAtUtc.Value, timeZone)}");
        }

        if (HasPaymentDifference(order.RequestedPaymentMethod, order.PaymentMethod))
        {
            notes.Add($"Alterado de {FormatPaymentMethod(order.RequestedPaymentMethod)} para {FormatPaymentMethod(order.PaymentMethod)}");
        }

        if (!string.IsNullOrWhiteSpace(order.PrintLastError))
        {
            notes.Add($"Falha de impressao: {order.PrintLastError.Trim()}");
        }

        return notes.Count == 0 ? null : string.Join(" | ", notes);
    }

    private static string? BuildOperationalOrderNotes(CustomerOrder order)
    {
        var notes = new List<string>();
        AppendOperationalNotes(notes, order);
        if (notes.Count == 0)
        {
            return null;
        }

        var combinedNotes = string.Join(" | ", notes);
        return combinedNotes.Length <= 600
            ? combinedNotes
            : $"{combinedNotes[..597]}...";
    }

    private static void AppendOperationalNotes(List<string> notes, CustomerOrder order)
    {
        if (!string.IsNullOrWhiteSpace(order.Notes))
        {
            notes.Add(order.Notes.Trim());
        }

        if (!order.DiningTable.IsDeliveryChannel)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(order.DeliveryPhone))
        {
            notes.Add($"Telefone: {order.DeliveryPhone.Trim()}");
        }

        if (IsPickupOrder(order))
        {
            notes.Add("Retirada: no local");
            return;
        }

        if (!string.IsNullOrWhiteSpace(order.DeliveryAddress) || !string.IsNullOrWhiteSpace(order.DeliveryNumber))
        {
            var deliveryAddress = $"{order.DeliveryAddress?.Trim() ?? string.Empty}, {order.DeliveryNumber?.Trim() ?? string.Empty}".Trim().TrimEnd(',').Trim();

            if (!string.IsNullOrWhiteSpace(order.DeliveryComplement))
            {
                deliveryAddress = $"{deliveryAddress} ({order.DeliveryComplement.Trim()})";
            }

            if (!string.IsNullOrWhiteSpace(deliveryAddress))
            {
                notes.Add($"Entrega: {deliveryAddress}");
            }
        }

        if (!string.IsNullOrWhiteSpace(order.DeliveryPostalCode))
        {
            notes.Add($"CEP: {order.DeliveryPostalCode}");
        }

        if (order.DeliveryFreightAmount > 0)
        {
            notes.Add($"Frete: {order.DeliveryFreightAmount.ToString("C", PtBrCulture)}");
        }
    }

    private static DeliveryFreightQuoteDto HidePublicFreightQuoteDetails(DeliveryFreightQuoteDto quote)
    {
        quote.OriginPostalCode = null;
        quote.DistanceKm = null;
        quote.BaseDistanceKm = 0;
        quote.ChargedDistanceKm = 0;
        quote.PricePerKm = 0;

        if (quote.IsAvailable)
        {
            quote.Message = quote.FreightAmount > 0
                ? "Taxa de entrega calculada para o CEP informado."
                : "Nenhuma taxa de entrega foi adicionada para este CEP.";
        }

        return quote;
    }

    private static bool HasPaymentDifference(PaymentMethod requestedPaymentMethod, PaymentMethod paymentMethod)
    {
        return requestedPaymentMethod != PaymentMethod.Undefined && paymentMethod != requestedPaymentMethod;
    }

    private static bool IsWithinBusinessDay(DateTime utcDateTime, DateTime dayStartUtc, DateTime dayEndUtc)
    {
        return utcDateTime >= dayStartUtc && utcDateTime < dayEndUtc;
    }

    private static bool IsWithinBusinessDay(DateTime? utcDateTime, DateTime dayStartUtc, DateTime dayEndUtc)
    {
        return utcDateTime.HasValue && IsWithinBusinessDay(utcDateTime.Value, dayStartUtc, dayEndUtc);
    }

    private static string FormatOrderStatus(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Pending => "Novo",
            OrderStatus.InKitchen => "Em preparo",
            OrderStatus.Ready => "Pronto",
            OrderStatus.Delivered => "Concluido",
            OrderStatus.Cancelled => "Cancelado",
            _ => status.ToString()
        };
    }

    private static string FormatPaymentMethod(PaymentMethod paymentMethod)
    {
        return paymentMethod switch
        {
            PaymentMethod.Undefined => "Escolher no caixa",
            PaymentMethod.Pix => "Pix",
            PaymentMethod.Credit => "Credito",
            PaymentMethod.Debit => "Debito",
            PaymentMethod.Cash => "Dinheiro",
            _ => paymentMethod.ToString()
        };
    }

    private static string FormatPaymentStatus(PaymentStatus paymentStatus)
    {
        return paymentStatus switch
        {
            PaymentStatus.Pending => "A cobrar",
            PaymentStatus.Paid => "Pago",
            _ => paymentStatus.ToString()
        };
    }

    private static string FormatBusinessDateTime(DateTime utcDateTime, TimeZoneInfo timeZone)
    {
        var localDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), timeZone);
        return localDateTime.ToString("dd/MM/yyyy HH:mm", PtBrCulture);
    }

    private static string FormatCurrencyValue(decimal value)
    {
        return value.ToString("C", PtBrCulture);
    }

    private static (DateTime DayStartUtc, DateTime DayEndUtc, DateTime LocalNow) GetCurrentBusinessDayRangeUtc(TimeZoneInfo timeZone)
    {
        var utcNow = DateTime.UtcNow;
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
        var localDayStart = new DateTime(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var localDayEnd = localDayStart.AddDays(1);
        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(localDayStart, timeZone);
        var dayEndUtc = TimeZoneInfo.ConvertTimeToUtc(localDayEnd, timeZone);
        return (dayStartUtc, dayEndUtc, localNow);
    }

    private static TimeZoneInfo GetBusinessTimeZone()
    {
        foreach (var timeZoneId in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }

    private async Task<string?> BuildPublicDeliveryCustomerUrlAsync(CustomerOrder order, CancellationToken cancellationToken)
    {
        if (!order.DiningTable.IsDeliveryChannel ||
            order.DiningTable.QrCodeAccess is null ||
            string.IsNullOrWhiteSpace(order.DeliveryPhone))
        {
            return null;
        }

        var baseUrl = string.IsNullOrWhiteSpace(_publicAppOptions.BaseUrl)
            ? "https://zeropaperflow.com.br"
            : _publicAppOptions.BaseUrl.Trim().TrimEnd('/');
        var normalizedPhone = DeliveryCustomerProfile.NormalizePhone(order.DeliveryPhone);
        var shortCode = await _deliveryCustomerLinkService.GetOrCreateShortCodeForCustomerAsync(
            order.CompanyId,
            normalizedPhone,
            cancellationToken);

        return string.IsNullOrWhiteSpace(shortCode)
            ? null
            : $"{baseUrl}/d/{Uri.EscapeDataString(shortCode)}";
    }

    private string? BuildDeliveryAssistantMessage(CustomerOrder order)
    {
        if (!order.DiningTable.IsDeliveryChannel)
        {
            return null;
        }

        var customerName = string.IsNullOrWhiteSpace(order.CustomerName) ? "cliente" : order.CustomerName.Trim();
        var isPickupOrder = IsPickupOrder(order);
        var addressLabel = $"{order.DeliveryAddress?.Trim() ?? string.Empty}, {order.DeliveryNumber?.Trim() ?? string.Empty}".Trim().TrimEnd(',').Trim();

        if (!string.IsNullOrWhiteSpace(order.DeliveryComplement))
        {
            addressLabel = string.IsNullOrWhiteSpace(addressLabel)
                ? order.DeliveryComplement.Trim()
                : $"{addressLabel} ({order.DeliveryComplement.Trim()})";
        }

        var firstParagraph = isPickupOrder
            ? $"Recebemos seu pedido para retirada, {customerName}. A unidade vai preparar para retirada no local."
            : $"Recebemos seu delivery, {customerName}. Entrega: {addressLabel}.";
        var freightParagraph = order.DeliveryFreightAmount > 0
            ? $"Taxa de entrega: {order.DeliveryFreightAmount.ToString("C", PtBrCulture)}. Total: {order.TotalAmount.ToString("C", PtBrCulture)}."
            : $"Total: {order.TotalAmount.ToString("C", PtBrCulture)}.";
        var secondParagraph = isPickupOrder
            ? "A unidade recebeu o pedido. Se precisar ajustar algo, responda no atendimento."
            : "A unidade recebeu o pedido e acompanha a entrega por aqui. Se precisar corrigir algo, responda no atendimento.";

        return $"{firstParagraph}\n\n{freightParagraph}\n\n{secondParagraph}";
    }

    private static string ResolveFulfillmentType(DiningTable table, string? fulfillmentType)
    {
        if (!table.IsDeliveryChannel)
        {
            return FulfillmentTypeLocal;
        }

        return IsPickupFulfillment(fulfillmentType)
            ? FulfillmentTypePickup
            : FulfillmentTypeDelivery;
    }

    private static string ResolveFulfillmentType(CustomerOrder order)
    {
        if (!order.DiningTable.IsDeliveryChannel)
        {
            return FulfillmentTypeLocal;
        }

        return IsPickupOrder(order) ? FulfillmentTypePickup : FulfillmentTypeDelivery;
    }

    private static bool IsPickupFulfillment(string? fulfillmentType)
    {
        if (string.IsNullOrWhiteSpace(fulfillmentType))
        {
            return false;
        }

        var normalized = fulfillmentType.Trim();
        return normalized.Equals(FulfillmentTypePickup, StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("retirada", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("pickup", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPickupOrder(CustomerOrder order)
    {
        return order.DiningTable.IsDeliveryChannel &&
               string.Equals(order.DeliveryAddress, PickupAddressMarker, StringComparison.OrdinalIgnoreCase) &&
               string.IsNullOrWhiteSpace(order.DeliveryPostalCode);
    }

    private static DateTime AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static DateTime? AsUtc(DateTime? value)
    {
        return value.HasValue ? AsUtc(value.Value) : null;
    }

    private static CustomerProfileDto MapCustomerProfile(DeliveryCustomerProfile profile)
    {
        return new CustomerProfileDto
        {
            Id = profile.Id,
            PhoneNumber = profile.Phone,
            Name = profile.CustomerName,
            ZipCode = profile.DeliveryPostalCode,
            Street = profile.DeliveryAddress,
            Number = profile.DeliveryNumber,
            Neighborhood = profile.DeliveryNeighborhood,
            Complement = profile.DeliveryComplement,
            CreatedAtUtc = profile.CreatedAtUtc,
            UpdatedAtUtc = profile.UpdatedAtUtc,
            LastOrderAtUtc = profile.LastOrderAtUtc
        };
    }

    private static PublicCustomerPrimaryAddressDto? MapPublicCustomerPrimaryAddress(DeliveryCustomerProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.DeliveryAddress) &&
            string.IsNullOrWhiteSpace(profile.DeliveryNumber) &&
            string.IsNullOrWhiteSpace(profile.DeliveryNeighborhood) &&
            string.IsNullOrWhiteSpace(profile.DeliveryComplement) &&
            string.IsNullOrWhiteSpace(profile.DeliveryPostalCode))
        {
            return null;
        }

        return new PublicCustomerPrimaryAddressDto
        {
            Street = profile.DeliveryAddress,
            Number = profile.DeliveryNumber,
            Neighborhood = profile.DeliveryNeighborhood,
            Complement = profile.DeliveryComplement,
            ZipCode = profile.DeliveryPostalCode
        };
    }

    private static PublicCustomerRecentOrderDto MapPublicCustomerRecentOrder(
        CustomerOrderHistory history,
        CustomerOrder? order)
    {
        var items = order is not null
            ? order.Items
                .OrderBy(item => item.CreatedAtUtc)
                .Select(item => new PublicCustomerRecentOrderItemDto
                {
                    Name = item.Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Total = item.TotalPrice
                })
                .ToList()
            : history.Items
                .OrderBy(item => item.CreatedAtUtc)
                .Select(item => new PublicCustomerRecentOrderItemDto
                {
                    Name = item.ItemName,
                    Quantity = item.Quantity
                })
                .ToList();

        return new PublicCustomerRecentOrderDto
        {
            OrderNumber = order?.Number,
            DisplayCode = order is null ? null : $"#{order.Number}",
            CreatedAt = AsUtc(order?.SubmittedAtUtc ?? history.CreatedAtUtc),
            Status = order?.Status.ToString() ?? "Recorded",
            Total = order?.TotalAmount ?? history.TotalAmount,
            FulfillmentType = order is null ? FulfillmentTypeDelivery : ResolveFulfillmentType(order),
            Items = items
        };
    }

    private static string MaskCustomerPhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length <= 4)
        {
            return "****";
        }

        var lastDigits = digits[^4..];
        return $"********{lastDigits}";
    }

    private CustomerOrderDto MapOrder(
        CustomerOrder order,
        string? tableName = null,
        string? publicEditUrl = null,
        string? deliveryAssistantMessage = null,
        bool includeDeliveryDistance = true,
        string? publicDeliveryCustomerUrl = null,
        bool includeItems = true)
    {
        var paymentTotalAmount = order.Payments.Sum(item => item.Amount);

        return new CustomerOrderDto
        {
            Id = order.Id,
            Number = order.Number,
            TableId = order.DiningTableId,
            TableName = tableName ?? order.DiningTable.Name,
            PublicCode = order.DiningTable.QrCodeAccess?.PublicCode,
            Status = order.Status.ToString(),
            PaymentMethod = order.PaymentMethod.ToString(),
            RequestedPaymentMethod = order.RequestedPaymentMethod.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            PrintStatus = order.PrintStatus.ToString(),
            CustomerName = order.CustomerName,
            Notes = order.Notes,
            IsDeliveryOrder = order.DiningTable.IsDeliveryChannel,
            FulfillmentType = ResolveFulfillmentType(order),
            DeliveryPhone = order.DeliveryPhone,
            DeliveryAddress = order.DeliveryAddress,
            DeliveryNumber = order.DeliveryNumber,
            DeliveryComplement = order.DeliveryComplement,
            DeliveryPostalCode = order.DeliveryPostalCode,
            DeliveryFreightAmount = order.DeliveryFreightAmount,
            DeliveryDistanceKm = includeDeliveryDistance ? order.DeliveryDistanceKm : null,
            DeliveryFreightProvider = order.DeliveryFreightProvider,
            DeliveryFreightCalculatedAtUtc = AsUtc(order.DeliveryFreightCalculatedAtUtc),
            CanEditPublicly = false,
            PublicEditAllowedUntilUtc = null,
            PublicEditUrl = null,
            PublicDeliveryCustomerUrl = publicDeliveryCustomerUrl,
            DeliveryAssistantMessage = deliveryAssistantMessage,
            OriginalTotalAmount = order.OriginalTotalAmount,
            TotalAmount = order.TotalAmount,
            TotalItemQuantity = order.Items.Sum(item => item.Quantity),
            IsEdited = order.IsEdited,
            EditedAtUtc = AsUtc(order.EditedAtUtc),
            DiscountAmount = order.DiscountAmount,
            SurchargeAmount = order.SurchargeAmount,
            CouponId = order.CouponId,
            CouponCode = order.CouponCode,
            CouponDiscountAmount = order.CouponDiscountAmount,
            CouponAppliedAtUtc = AsUtc(order.CouponAppliedAtUtc),
            PriceAdjustmentNote = order.PriceAdjustmentNote,
            PriceAdjustedAtUtc = AsUtc(order.PriceAdjustedAtUtc),
            HasPriceAdjustment = order.DiscountAmount > 0 || order.SurchargeAmount > 0 || order.PriceAdjustedAtUtc.HasValue,
            PaymentTotalAmount = paymentTotalAmount,
            RemainingPaymentAmount = Math.Max(0m, order.TotalAmount - paymentTotalAmount),
            SubmittedAtUtc = AsUtc(order.SubmittedAtUtc),
            PaidAtUtc = AsUtc(order.PaidAtUtc),
            PrintedAtUtc = AsUtc(order.PrintedAtUtc),
            PrintAttempts = order.PrintAttempts,
            PrintLastError = order.PrintLastError,
            PrintAgentName = order.PrintAgentName,
            PrintPrinterName = order.PrintPrinterName,
            Payments = order.Payments
                .OrderBy(item => item.CreatedAtUtc)
                .Select(item => new OrderPaymentDto
                {
                    Id = item.Id,
                    Method = item.Method.ToString(),
                    Amount = item.Amount,
                    CreatedAtUtc = AsUtc(item.CreatedAtUtc)
                })
                .ToList(),
            Items = includeItems ? order.Items.Select(item => new OrderItemDto
            {
                Id = item.Id,
                MenuItemId = item.SourceMenuItemId,
                Name = item.Name,
                CategoryName = item.CategoryName,
                ImageUrl = NormalizeMenuImagePath(item.ImageUrl),
                Quantity = item.Quantity,
                BaseUnitPrice = item.BaseUnitPrice,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice,
                Notes = item.Notes,
                AdditionalSelections = item.AdditionalSelections.Select(selection => new OrderItemAdditionalSelectionDto
                {
                    SourceMenuItemAdditionalOptionId = selection.SourceMenuItemAdditionalOptionId,
                    GroupName = selection.GroupName,
                    OptionName = selection.OptionName,
                    UnitPrice = selection.UnitPrice
                }).ToList()
            }).ToList() : []
        };
    }

    private static StockItemDto MapStockItem(StockItem item)
    {
        return new StockItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Category = item.Category,
            Unit = item.Unit,
            CurrentQuantity = item.CurrentQuantity,
            MinimumQuantity = item.MinimumQuantity,
            IsLowStock = item.CurrentQuantity <= item.MinimumQuantity
        };
    }

    private static CompanySettingsDto MapCompanySettings(Company company, AppUser user, DateTime utcNow)
    {
        return new CompanySettingsDto
        {
            LegalName = company.LegalName,
            TradeName = company.TradeName,
            LogoUrl = NormalizeMenuImagePath(company.LogoUrl),
            AccessSlug = company.AccessSlug,
            ContactEmail = company.ContactEmail,
            ContactPhone = company.ContactPhone,
            Alerts = MapAlertSettings(company),
            ShortcutAccess = MapOwnerShortcutAccess(user, utcNow)
        };
    }

    private async Task<AppUser> GetCurrentUserAsync(
        WorkspaceSessionContext session,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = asNoTracking ? _context.Users.AsNoTracking() : _context.Users.AsQueryable();

        return await query
            .FirstOrDefaultAsync(
                item => item.Id == session.UserId &&
                        item.TenantId == session.TenantId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Perfil do owner nao encontrado.");
    }

    private void VerifyOwnerShortcutPassword(string? password, AppUser user)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Informe a senha owner para gerar ou revogar o atalho.", nameof(password));
        }

        if (!_passwordHasher.Verify(password, user.PasswordHash))
        {
            throw new InvalidOperationException("Senha owner incorreta.");
        }
    }

    private static void EnsureOwnerShortcutAllowed(WorkspaceSessionContext session)
    {
        if (!string.Equals(session.Role, UserRole.Owner.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Apenas o owner da unidade pode gerar atalhos automaticos.");
        }
    }

    private string BuildOwnerShortcutUrl(string rawToken)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_publicAppOptions.BaseUrl)
            ? "https://zeropaperflow.com.br"
            : _publicAppOptions.BaseUrl.Trim().TrimEnd('/');

        return $"{baseUrl}/login/atalho#token={Uri.EscapeDataString(rawToken)}";
    }

    private static OwnerShortcutAccessDto MapOwnerShortcutAccess(AppUser user, DateTime utcNow)
    {
        return new OwnerShortcutAccessDto
        {
            IsEnabled = user.HasActiveShortcutAccess(utcNow),
            CreatedAtUtc = user.ShortcutAccessCreatedAtUtc,
            ExpiresAtUtc = user.ShortcutAccessExpiresAtUtc,
            LastUsedAtUtc = user.ShortcutAccessLastUsedAtUtc
        };
    }

    private static string ComputeShortcutAccessTokenHash(string rawToken)
    {
        var tokenBytes = System.Text.Encoding.UTF8.GetBytes(rawToken.Trim());
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToHexString(hashBytes);
    }

    private static OwnerProfileDto MapOwnerProfile(AppUser user)
    {
        return new OwnerProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString()
        };
    }

    private static string NormalizeOwnerFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Informe o nome do owner.", nameof(fullName));
        }

        var normalized = fullName.Trim();
        if (normalized.Length > 150)
        {
            throw new ArgumentException("O nome do owner precisa ter no maximo 150 caracteres.", nameof(fullName));
        }

        return normalized;
    }

    private static string NormalizeOwnerEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Informe o email do owner.", nameof(email));
        }

        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length > 180)
        {
            throw new ArgumentException("O email do owner precisa ter no maximo 180 caracteres.", nameof(email));
        }

        try
        {
            var address = new MailAddress(normalized);
            if (!string.Equals(address.Address, normalized, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Informe um email valido.", nameof(email));
            }
        }
        catch (FormatException)
        {
            throw new ArgumentException("Informe um email valido.", nameof(email));
        }

        return normalized;
    }

    private static DiningTableDto MapDiningTable(
        DiningTable table,
        string? publicCode = null,
        string? accessUrl = null,
        int? openOrderCount = null)
    {
        return new DiningTableDto
        {
            Id = table.Id,
            Name = table.Name,
            InternalCode = table.InternalCode,
            ComandaLabel = table.ComandaLabel,
            IsDeliveryChannel = table.IsDeliveryChannel,
            Seats = table.Seats,
            Status = table.Status.ToString(),
            OpenOrderCount = openOrderCount ?? table.Orders.Count(order =>
                order.IsActive &&
                order.Status != OrderStatus.Delivered &&
                order.Status != OrderStatus.Cancelled),
            PublicCode = publicCode ?? table.QrCodeAccess.PublicCode,
            AccessUrl = accessUrl ?? table.QrCodeAccess.AccessPath,
            AlertSoundUrl = NormalizeAlertSoundPath(table.AlertSoundUrl),
            HasCustomAlertSound = !string.IsNullOrWhiteSpace(table.AlertSoundUrl)
        };
    }

    private static AlertSettingsDto MapAlertSettings(Company company)
    {
        return new AlertSettingsDto
        {
            EnableOrderAlerts = company.EnableOrderAlerts,
            EnableWaiterCallAlerts = company.EnableWaiterCallAlerts,
            SoundUrl = NormalizeAlertSoundPath(company.AlertSoundUrl),
            HasCustomSound = !string.IsNullOrWhiteSpace(company.AlertSoundUrl),
            VolumePercent = company.AlertVolumePercent,
            PlaybackSeconds = company.AlertPlaybackSeconds
        };
    }

    private static WaiterCallDto MapWaiterCall(WaiterCall waiterCall, string? tableName = null)
    {
        return new WaiterCallDto
        {
            Id = waiterCall.Id,
            TableId = waiterCall.DiningTableId,
            TableName = tableName ?? waiterCall.DiningTable.Name,
            TableAlertSoundUrl = NormalizeAlertSoundPath(waiterCall.DiningTable.AlertSoundUrl),
            RequestedAtUtc = waiterCall.RequestedAtUtc,
            ResolvedAtUtc = waiterCall.ResolvedAtUtc
        };
    }

    private static OrderStatus ParseOrderStatus(string value)
    {
        if (!Enum.TryParse<OrderStatus>(value, true, out var parsed))
        {
            throw new ArgumentException("Invalid order status.", nameof(value));
        }

        return parsed;
    }

    private static PaymentMethod ParsePaymentMethod(string value)
    {
        if (!Enum.TryParse<PaymentMethod>(value, true, out var parsed))
        {
            throw new ArgumentException("Invalid payment method.", nameof(value));
        }

        return parsed;
    }

    private static PaymentMethod ParsePaymentMethodOrDefault(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return PaymentMethod.Undefined;
        }

        return ParsePaymentMethod(value);
    }

    private static string NormalizePostalCodeOrThrow(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Informe um CEP valido com 8 digitos.", fieldName);
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length != 8)
        {
            throw new ArgumentException("Informe um CEP valido com 8 digitos.", fieldName);
        }

        return digits;
    }

    private static int? NormalizeMaxAdditionalSelections(int? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        if (value.Value is < 0 or > 100)
        {
            throw new ArgumentException("O limite de adicionais precisa ficar entre 0 e 100.", nameof(value));
        }

        return value.Value;
    }

    private static string NormalizePhoneForDeliveryCustomer(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length is 10 or 11)
        {
            return $"55{digits}";
        }

        return digits;
    }

    private static string NormalizeCustomerProfilePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Informe um telefone valido.", nameof(value));
        }

        var normalized = DeliveryCustomerProfile.NormalizePhone(value);
        if (normalized.Length is < 10 or > 14)
        {
            throw new ArgumentException("Informe um telefone valido.", nameof(value));
        }

        return normalized;
    }

    private static string? NormalizeOptionalCustomerText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string? NormalizeOptionalPostalCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length != 8)
        {
            throw new ArgumentException("Informe um CEP valido com 8 digitos.", nameof(value));
        }

        return digits;
    }

    private static PaymentStatus ParsePaymentStatus(string value)
    {
        if (!Enum.TryParse<PaymentStatus>(value, true, out var parsed))
        {
            throw new ArgumentException("Invalid payment status.", nameof(value));
        }

        return parsed;
    }

    private static UserRole ParseUserRole(string value)
    {
        if (!Enum.TryParse<UserRole>(value, true, out var parsed))
        {
            throw new ArgumentException("Invalid user role.", nameof(value));
        }

        return parsed;
    }

    private static MenuItemDto MapMenuItem(MenuItem item)
    {
        return new MenuItemDto
        {
            Id = item.Id,
            CategoryId = item.MenuCategoryId,
            Name = item.Name,
            Description = item.Description,
            AccentLabel = item.AccentLabel,
            ImageUrl = NormalizeMenuImagePath(item.ImageUrl),
            Price = item.Price,
            StartingPrice = CalculateMenuItemStartingPrice(item),
            DisplayOrder = item.DisplayOrder,
            IsActive = item.IsActive,
            MaxAdditionalSelections = item.MaxAdditionalSelections,
            HasAdditionalOptions = item.AdditionalGroups
                .Any(group =>
                    group.IsActive &&
                    group.MaxAdditionalSelections != 0 &&
                    group.Options.Any(option => option.IsActive)),
            AdditionalGroups = item.AdditionalGroups
                .Where(group => group.IsActive)
                .OrderBy(group => group.DisplayOrder)
                .ThenBy(group => group.Name)
                .Select(group => new MenuItemAdditionalGroupDto
                {
                    Id = group.Id,
                    SourceMenuAdditionalCatalogGroupId = group.SourceMenuAdditionalCatalogGroupId,
                    Name = group.Name,
                    AllowMultiple = group.AllowMultiple,
                    DisplayOrder = group.DisplayOrder,
                    MaxAdditionalSelections = group.MaxAdditionalSelections,
                    Options = group.Options
                        .Where(option => option.IsActive)
                        .OrderBy(option => option.DisplayOrder)
                        .ThenBy(option => option.Name)
                        .Select(option => new MenuItemAdditionalOptionDto
                        {
                            Id = option.Id,
                            GroupId = option.MenuItemAdditionalGroupId,
                            SourceMenuAdditionalCatalogOptionId = option.SourceMenuAdditionalCatalogOptionId,
                            Name = option.Name,
                            Price = option.Price,
                            DisplayOrder = option.DisplayOrder
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    private static MenuItemDto MapPublicMenuItem(MenuItem item, bool hasAdditionalOptions, decimal startingPrice)
    {
        return new MenuItemDto
        {
            Id = item.Id,
            CategoryId = item.MenuCategoryId,
            Name = item.Name,
            Description = item.Description,
            AccentLabel = item.AccentLabel,
            ImageUrl = NormalizeMenuImagePath(item.ImageUrl),
            Price = item.Price,
            StartingPrice = startingPrice,
            DisplayOrder = item.DisplayOrder,
            IsActive = item.IsActive,
            MaxAdditionalSelections = item.MaxAdditionalSelections,
            HasAdditionalOptions = hasAdditionalOptions
        };
    }

    private static decimal CalculateMenuItemStartingPrice(MenuItem item)
    {
        if (item.Price > 0)
        {
            return item.Price;
        }

        return item.AdditionalGroups
            .Where(group => group.IsActive && group.MaxAdditionalSelections != 0)
            .SelectMany(group => group.Options)
            .Where(option => option.IsActive && option.Price > 0)
            .Select(option => option.Price)
            .DefaultIfEmpty(item.Price)
            .Min();
    }

    private static MenuAdditionalCatalogGroupDto MapMenuAdditionalCatalogGroup(MenuAdditionalCatalogGroup item)
    {
        return new MenuAdditionalCatalogGroupDto
        {
            Id = item.Id,
            Name = item.Name,
            AllowMultiple = item.AllowMultiple,
            DisplayOrder = item.DisplayOrder,
            MaxAdditionalSelections = item.MaxAdditionalSelections,
            Options = item.Options
                .Where(option => option.IsActive)
                .OrderBy(option => option.DisplayOrder)
                .ThenBy(option => option.Name)
                .Select(option => new MenuAdditionalCatalogOptionDto
                {
                    Id = option.Id,
                    GroupId = option.MenuAdditionalCatalogGroupId,
                    Name = option.Name,
                    Price = option.Price,
                    DisplayOrder = option.DisplayOrder
                })
            .ToList()
        };
    }

    private Task<MenuAdditionalCatalogGroup?> GetMenuAdditionalCatalogGroupEntityAsync(Guid companyId, Guid groupId, CancellationToken cancellationToken)
    {
        return _context.MenuAdditionalCatalogGroups
            .AsSplitQuery()
            .Include(item => item.Options)
            .FirstOrDefaultAsync(
                item => item.Id == groupId &&
                        item.CompanyId == companyId &&
                        item.IsActive,
                cancellationToken);
    }

    private Task<MenuItem?> GetMenuItemEntityAsync(Guid companyId, Guid menuItemId, CancellationToken cancellationToken)
    {
        return _context.MenuItems
            .AsSplitQuery()
            .Include(item => item.AdditionalGroups)
                .ThenInclude(item => item.Options)
            .FirstOrDefaultAsync(
                item => item.Id == menuItemId &&
                        item.CompanyId == companyId,
                cancellationToken);
    }

    private async Task SyncLinkedMenuItemAdditionalGroupsAsync(
        Guid companyId,
        MenuAdditionalCatalogGroup catalogGroup,
        CancellationToken cancellationToken)
    {
        var linkedGroups = await _context.MenuItemAdditionalGroups
            .Where(group =>
                group.CompanyId == companyId &&
                group.SourceMenuAdditionalCatalogGroupId == catalogGroup.Id)
            .ToListAsync(cancellationToken);

        if (linkedGroups.Count == 0)
        {
            return;
        }

        foreach (var linkedGroup in linkedGroups)
        {
            linkedGroup.Rename(catalogGroup.Name);
            linkedGroup.SetAllowMultiple(catalogGroup.AllowMultiple);
            linkedGroup.SetMaxAdditionalSelections(catalogGroup.MaxAdditionalSelections);
            linkedGroup.SetCatalogSource(catalogGroup.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var linkedGroupIds = linkedGroups.Select(group => group.Id).ToList();

        await _context.MenuItemAdditionalOptions
            .Where(option => linkedGroupIds.Contains(option.MenuItemAdditionalGroupId))
            .ExecuteDeleteAsync(cancellationToken);

        var catalogOptions = catalogGroup.Options
            .Where(option => option.IsActive)
            .OrderBy(option => option.DisplayOrder)
            .ThenBy(option => option.Name)
            .ToList();

        var replacementOptions = linkedGroups
            .SelectMany(linkedGroup => catalogOptions.Select((catalogOption, optionIndex) =>
                new MenuItemAdditionalOption(
                    linkedGroup.TenantId,
                    linkedGroup.CompanyId,
                    linkedGroup.MenuItemId,
                    linkedGroup.Id,
                    catalogOption.Name,
                    catalogOption.Price,
                    optionIndex,
                    catalogOption.Id)))
            .ToList();

        if (replacementOptions.Count > 0)
        {
            await _context.MenuItemAdditionalOptions.AddRangeAsync(replacementOptions, cancellationToken);
        }
    }

    private async Task<List<MenuItemAdditionalGroup>> BuildAdditionalGroupsAsync(
        WorkspaceSessionContext session,
        Guid menuItemId,
        IEnumerable<MenuItemAdditionalGroupInputDto>? additionalGroups,
        CancellationToken cancellationToken)
    {
        if (additionalGroups is null)
        {
            return [];
        }

        var groupInputs = additionalGroups
            .Where(group => group.CatalogGroupId.HasValue || !string.IsNullOrWhiteSpace(group.Name))
            .ToList();

        if (groupInputs.Count == 0)
        {
            return [];
        }

        var catalogGroupIds = groupInputs
            .Select(group => group.CatalogGroupId)
            .Where(groupId => groupId.HasValue && groupId.Value != Guid.Empty)
            .Select(groupId => groupId!.Value)
            .Distinct()
            .ToList();

        var catalogGroups = catalogGroupIds.Count == 0
            ? new Dictionary<Guid, MenuAdditionalCatalogGroup>()
            : await _context.MenuAdditionalCatalogGroups
                .AsNoTracking()
                .AsSplitQuery()
                .Include(group => group.Options)
                .Where(group =>
                    catalogGroupIds.Contains(group.Id) &&
                    group.CompanyId == session.CompanyId &&
                    group.IsActive)
                .ToDictionaryAsync(group => group.Id, cancellationToken);

        if (catalogGroups.Count != catalogGroupIds.Count)
        {
            throw new KeyNotFoundException("Um ou mais complementos vinculados nao foram encontrados.");
        }

        return groupInputs
            .Select((group, groupIndex) =>
            {
                catalogGroups.TryGetValue(group.CatalogGroupId ?? Guid.Empty, out var catalogGroup);

                var additionalGroup = new MenuItemAdditionalGroup(
                    session.TenantId,
                    session.CompanyId,
                    menuItemId,
                    catalogGroup?.Name ?? group.Name,
                    catalogGroup?.AllowMultiple ?? group.AllowMultiple,
                    groupIndex,
                    NormalizeMaxAdditionalSelections(catalogGroup?.MaxAdditionalSelections ?? group.MaxAdditionalSelections),
                    catalogGroup?.Id);

                var optionsSource = catalogGroup?.Options
                    .Where(option => option.IsActive)
                    .OrderBy(option => option.DisplayOrder)
                    .ThenBy(option => option.Name)
                    .Select(option => new MenuItemAdditionalOptionInputDto
                    {
                        CatalogOptionId = option.Id,
                        Name = option.Name,
                        Price = option.Price
                    })
                    .ToList() ?? group.Options;

                var options = optionsSource
                    .Where(option => !string.IsNullOrWhiteSpace(option.Name))
                    .Select((option, optionIndex) =>
                        new MenuItemAdditionalOption(
                            session.TenantId,
                            session.CompanyId,
                            menuItemId,
                            additionalGroup.Id,
                            option.Name,
                            option.Price,
                            optionIndex,
                            option.CatalogOptionId))
                    .ToList();

                additionalGroup.ReplaceOptions(options);
                return additionalGroup;
            })
            .Where(group => group.Options.Count > 0)
            .ToList();
    }

    private static List<MenuAdditionalCatalogOption> BuildMenuAdditionalCatalogOptions(
        WorkspaceSessionContext session,
        Guid groupId,
        IEnumerable<SaveMenuAdditionalCatalogOptionRequestDto>? options)
    {
        if (options is null)
        {
            return [];
        }

        return options
            .Where(option => !string.IsNullOrWhiteSpace(option.Name))
            .Select((option, optionIndex) =>
                new MenuAdditionalCatalogOption(
                    session.TenantId,
                    session.CompanyId,
                    groupId,
                    option.Name,
                    option.Price,
                    optionIndex))
            .ToList();
    }

    private static List<OrderItemAdditionalSelection> BuildOrderItemAdditionalSelections(
        MenuItem menuItem,
        IEnumerable<Guid>? additionalOptionIds,
        Guid tenantId)
    {
        var normalizedOptionIds = additionalOptionIds?
            .Where(optionId => optionId != Guid.Empty)
            .Distinct()
            .ToHashSet() ?? [];

        if (normalizedOptionIds.Count == 0)
        {
            return [];
        }

        var availableGroups = menuItem.AdditionalGroups
            .Where(group => group.IsActive)
            .ToList();

        var selectedOptions = availableGroups
            .SelectMany(group => group.Options.Where(option => option.IsActive && normalizedOptionIds.Contains(option.Id)))
            .ToList();

        if (menuItem.MaxAdditionalSelections.HasValue &&
            selectedOptions.Count > menuItem.MaxAdditionalSelections.Value)
        {
            var message = menuItem.MaxAdditionalSelections.Value == 0
                ? $"O item {menuItem.Name} nao aceita adicionais."
                : $"Escolha no maximo {menuItem.MaxAdditionalSelections.Value} adicionais para o item {menuItem.Name}.";
            throw new ArgumentException(message);
        }

        if (selectedOptions.Count != normalizedOptionIds.Count)
        {
            throw new ArgumentException($"Um ou mais adicionais nao pertencem ao item {menuItem.Name}.");
        }

        foreach (var group in availableGroups.Where(group => !group.AllowMultiple))
        {
            var selectedCount = selectedOptions.Count(option => option.MenuItemAdditionalGroupId == group.Id);

            if (selectedCount > 1)
            {
                throw new ArgumentException($"Escolha apenas uma opcao para o grupo {group.Name}.");
            }
        }

        foreach (var group in availableGroups.Where(group => group.MaxAdditionalSelections.HasValue))
        {
            var selectedCount = selectedOptions.Count(option => option.MenuItemAdditionalGroupId == group.Id);

            if (selectedCount > group.MaxAdditionalSelections!.Value)
            {
                throw new ArgumentException(
                    $"Escolha no maximo {group.MaxAdditionalSelections.Value} adicional(is) em {group.Name}.");
            }
        }

        return selectedOptions
            .OrderBy(option => option.MenuItemAdditionalGroup.DisplayOrder)
            .ThenBy(option => option.DisplayOrder)
            .Select(option =>
                new OrderItemAdditionalSelection(
                    tenantId,
                    option.MenuItemAdditionalGroup.Name,
                    option.Name,
                    option.Price,
                    option.Id))
            .ToList();
    }

    private static string? NormalizeMenuImagePath(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        var normalized = imageUrl.Trim();

        try
        {
            normalized = Uri.UnescapeDataString(normalized);
        }
        catch
        {
            // Mantem o valor original se vier com escape invalido.
        }

        if (Uri.TryCreate(normalized, UriKind.Absolute, out var absoluteUri))
        {
            var absolutePath = absoluteUri.AbsolutePath;

            if (absolutePath.StartsWith("/media/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                return absolutePath["/media".Length..];
            }

            if (absolutePath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                return absolutePath;
            }

            return normalized;
        }

        var queryIndex = normalized.IndexOfAny(['?', '#']);
        if (queryIndex >= 0)
        {
            normalized = normalized[..queryIndex];
        }

        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        if (normalized.StartsWith("/media/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            return normalized["/media".Length..];
        }

        if (normalized.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        return normalized;
    }

    private static string? NormalizeAlertSoundPath(string? soundUrl)
    {
        if (string.IsNullOrWhiteSpace(soundUrl))
        {
            return null;
        }

        var normalized = soundUrl.Trim();

        if (Uri.TryCreate(normalized, UriKind.Absolute, out var absoluteUri))
        {
            var pathAndQuery = absoluteUri.PathAndQuery;

            if (pathAndQuery.StartsWith("/media/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                return pathAndQuery["/media".Length..];
            }

            if (pathAndQuery.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                return pathAndQuery;
            }

            return normalized;
        }

        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        if (normalized.StartsWith("/media/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            return normalized["/media".Length..];
        }

        return normalized;
    }

    private sealed record PublicOrderingStatus(bool IsOpen, string? Message);
}
