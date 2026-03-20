using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly ZeroPaperDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IWebHostEnvironment _environment;

    public WorkspaceService(ZeroPaperDbContext context, IPasswordHasher passwordHasher, IWebHostEnvironment environment)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _environment = environment;
    }

    public async Task<WorkspaceOverviewDto> GetOverviewAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        var activeTables = await _context.DiningTables
            .CountAsync(
                item => item.CompanyId == session.CompanyId &&
                        item.IsActive &&
                        item.Status != TableStatus.Inactive,
                cancellationToken);

        var openOrders = await _context.CustomerOrders
            .CountAsync(
                item => item.CompanyId == session.CompanyId &&
                        item.IsActive &&
                        item.Status != OrderStatus.Delivered &&
                        item.Status != OrderStatus.Cancelled,
                cancellationToken);

        var publishedMenuItems = await _context.MenuItems
            .CountAsync(item => item.CompanyId == session.CompanyId && item.IsActive, cancellationToken);

        var totalMenuItems = await _context.MenuItems
            .CountAsync(item => item.CompanyId == session.CompanyId, cancellationToken);

        return new WorkspaceOverviewDto
        {
            ActiveTables = activeTables,
            OpenOrders = openOrders,
            PublishedMenuItems = publishedMenuItems,
            TotalMenuItems = totalMenuItems
        };
    }

    public async Task<IReadOnlyList<MenuCategoryDto>> GetMenuAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        return await BuildMenuAsync(session.CompanyId, includeInactiveItems: true, cancellationToken);
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
            nextDisplayOrder + 1);

        await _context.MenuCategories.AddAsync(category, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new MenuCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            DisplayOrder = category.DisplayOrder
        };
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

        var menuItem = new MenuItem(
            session.TenantId,
            session.CompanyId,
            category.Id,
            request.Name,
            request.Price,
            request.Description,
            request.AccentLabel,
            request.ImageUrl,
            nextDisplayOrder + 1);

        await _context.MenuItems.AddAsync(menuItem, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapMenuItem(menuItem);
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
        return MapMenuItem(menuItem);
    }

    public async Task<UploadMenuItemImageResponseDto> UploadMenuItemImageAsync(WorkspaceSessionContext session, IFormFile file, CancellationToken cancellationToken = default)
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

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        var safeExtension = extension is ".jpg" or ".jpeg" or ".png" or ".webp"
            ? extension
            : file.ContentType switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg"
            };

        var webRootPath = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;

        var relativeDirectory = Path.Combine("uploads", "menu", session.CompanyId.ToString("N"));
        var directoryPath = Path.Combine(webRootPath, relativeDirectory);
        Directory.CreateDirectory(directoryPath);

        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var filePath = Path.Combine(directoryPath, fileName);

        await using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var relativePath = "/" + Path.Combine(relativeDirectory, fileName).Replace("\\", "/");

        return new UploadMenuItemImageResponseDto
        {
            ImageUrl = relativePath
        };
    }

    public async Task DeleteMenuCategoryAsync(WorkspaceSessionContext session, Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = await _context.MenuCategories
            .Include(item => item.Items)
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
        return await _context.DiningTables
            .AsNoTracking()
            .Where(item => item.CompanyId == session.CompanyId && item.IsActive)
            .OrderBy(item => item.InternalCode)
            .Select(item => new DiningTableDto
            {
                Id = item.Id,
                Name = item.Name,
                InternalCode = item.InternalCode,
                Seats = item.Seats,
                Status = item.Status.ToString(),
                PublicCode = item.QrCodeAccess.PublicCode,
                AccessUrl = item.QrCodeAccess.AccessPath,
                OpenOrderCount = item.Orders.Count(order =>
                    order.IsActive &&
                    order.Status != OrderStatus.Delivered &&
                    order.Status != OrderStatus.Cancelled)
            })
            .ToListAsync(cancellationToken);
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
            request.Seats);

        await _context.QrCodeAccesses.AddAsync(qrCodeAccess, cancellationToken);
        await _context.DiningTables.AddAsync(table, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new DiningTableDto
        {
            Id = table.Id,
            Name = table.Name,
            InternalCode = table.InternalCode,
            Seats = table.Seats,
            Status = table.Status.ToString(),
            OpenOrderCount = 0,
            PublicCode = qrCodeAccess.PublicCode,
            AccessUrl = qrCodeAccess.AccessPath
        };
    }

    public async Task<IReadOnlyList<CustomerOrderDto>> GetOrdersAsync(WorkspaceSessionContext session, bool kitchenOnly, CancellationToken cancellationToken = default)
    {
        var query = _context.CustomerOrders
            .AsNoTracking()
            .Include(item => item.Items)
            .Include(item => item.DiningTable)
            .Where(item => item.CompanyId == session.CompanyId && item.IsActive);

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

        return orders.Select(order => MapOrder(order)).ToList();
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

        return await CreateOrderForTableAsync(
            table,
            request.CustomerName,
            request.Notes,
            request.Items,
            request.MenuSelections,
            cancellationToken);
    }

    public async Task<CustomerOrderDto> UpdateOrderStatusAsync(WorkspaceSessionContext session, Guid orderId, UpdateOrderStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var order = await _context.CustomerOrders
            .Include(item => item.Items)
            .Include(item => item.DiningTable)
            .FirstOrDefaultAsync(
                item => item.Id == orderId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");

        var nextStatus = ParseOrderStatus(request.Status);

        switch (nextStatus)
        {
            case OrderStatus.Pending:
                break;
            case OrderStatus.InKitchen:
                order.MoveToKitchen();
                order.DiningTable.ChangeStatus(TableStatus.Occupied);
                break;
            case OrderStatus.Ready:
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
                throw new ArgumentOutOfRangeException(nameof(request.Status), "Unsupported order status.");
        }

        await _context.SaveChangesAsync(cancellationToken);
        return MapOrder(order);
    }

    public async Task DeleteOrderAsync(WorkspaceSessionContext session, Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _context.CustomerOrders
            .Include(item => item.Items)
            .Include(item => item.DiningTable)
            .FirstOrDefaultAsync(
                item => item.Id == orderId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Pedido nao encontrado.");

        if (order.Status != OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("So pedidos cancelados podem ser removidos.");
        }

        _context.OrderItems.RemoveRange(order.Items);
        _context.CustomerOrders.Remove(order);
        await RecalculateTableStatusAsync(order.DiningTable, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
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

        return MapCompanySettings(company);
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
        return MapCompanySettings(company);
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

        table.QrCodeAccess.RegisterScan();
        await _context.SaveChangesAsync(cancellationToken);

        return new PublicTableViewDto
        {
            RestaurantName = table.Company.TradeName,
            TableName = table.Name,
            AccessCode = table.QrCodeAccess.PublicCode,
            Menu = await BuildMenuAsync(table.CompanyId, includeInactiveItems: false, cancellationToken)
        };
    }

    public async Task<CustomerOrderDto> CreatePublicOrderAsync(string publicCode, CreateCustomerOrderRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
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

        return await CreateOrderForTableAsync(
            table,
            request.CustomerName,
            request.Notes,
            request.Items,
            request.MenuSelections,
            cancellationToken);
    }

    private async Task<CustomerOrderDto> CreateOrderForTableAsync(
        DiningTable table,
        string? customerName,
        string? notes,
        List<OrderItemInputDto> items,
        List<MenuOrderSelectionDto> menuSelections,
        CancellationToken cancellationToken)
    {
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

        return await PersistOrderAsync(table, customerName, notes, orderItems, cancellationToken);
    }

    private async Task RecalculateTableStatusAsync(DiningTable table, CancellationToken cancellationToken)
    {
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

    private async Task<int> GetNextOrderNumberAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var maxNumber = await _context.CustomerOrders
            .Where(item => item.CompanyId == companyId)
            .Select(item => (int?)item.Number)
            .MaxAsync(cancellationToken);

        return (maxNumber ?? 0) + 1;
    }

    private async Task<List<MenuCategoryDto>> BuildMenuAsync(Guid companyId, bool includeInactiveItems, CancellationToken cancellationToken)
    {
        var categories = await _context.MenuCategories
            .AsNoTracking()
            .Where(item => item.CompanyId == companyId && item.IsActive)
            .Include(item => item.Items)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

        return categories.Select(item => new MenuCategoryDto
        {
            Id = item.Id,
            Name = item.Name,
            DisplayOrder = item.DisplayOrder,
            Items = item.Items
                .Where(menuItem => includeInactiveItems || menuItem.IsActive)
                .OrderBy(menuItem => menuItem.DisplayOrder)
                .ThenBy(menuItem => menuItem.Name)
                .Select(MapMenuItem)
                .ToList()
        }).ToList();
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
                .Where(item => item.CompanyId == companyId && item.IsActive && menuItemIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, cancellationToken);

            return menuSelections
                .Where(item => item.Quantity > 0 && menuItems.ContainsKey(item.MenuItemId))
                .Select(item =>
                {
                    var menuItem = menuItems[item.MenuItemId];
                    return new OrderItem(
                        tenantId,
                        menuItem.Name,
                        item.Quantity,
                        menuItem.Price,
                        item.Notes);
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
                    item.Notes))
            .ToList();
    }

    private async Task<CustomerOrderDto> PersistOrderAsync(
        DiningTable table,
        string? customerName,
        string? notes,
        List<OrderItem> orderItems,
        CancellationToken cancellationToken)
    {
        var nextNumber = await GetNextOrderNumberAsync(table.CompanyId, cancellationToken);
        var order = new CustomerOrder(
            table.TenantId,
            table.CompanyId,
            table.Id,
            nextNumber,
            customerName,
            notes,
            orderItems);

        table.ChangeStatus(TableStatus.Occupied);

        await _context.CustomerOrders.AddAsync(order, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapOrder(order, table.Name);
    }

    private static CustomerOrderDto MapOrder(CustomerOrder order, string? tableName = null)
    {
        return new CustomerOrderDto
        {
            Id = order.Id,
            Number = order.Number,
            TableId = order.DiningTableId,
            TableName = tableName ?? order.DiningTable.Name,
            Status = order.Status.ToString(),
            CustomerName = order.CustomerName,
            Notes = order.Notes,
            TotalAmount = order.TotalAmount,
            SubmittedAtUtc = order.SubmittedAtUtc,
            Items = order.Items.Select(item => new OrderItemDto
            {
                Id = item.Id,
                Name = item.Name,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice,
                Notes = item.Notes
            }).ToList()
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

    private static CompanySettingsDto MapCompanySettings(Company company)
    {
        return new CompanySettingsDto
        {
            LegalName = company.LegalName,
            TradeName = company.TradeName,
            AccessSlug = company.AccessSlug,
            ContactEmail = company.ContactEmail,
            ContactPhone = company.ContactPhone
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
            ImageUrl = item.ImageUrl,
            Price = item.Price,
            DisplayOrder = item.DisplayOrder,
            IsActive = item.IsActive
        };
    }
}
