using System.Globalization;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Documents;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class WorkspaceService : IWorkspaceService
{
    private static readonly CultureInfo PtBrCulture = new("pt-BR");
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
            FailedPrints = failedPrints
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
            nextDisplayOrder + 1);

        await _context.MenuItems.AddAsync(menuItem, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapMenuItem(menuItem);
    }

    public async Task<MenuItemDto> UpdateMenuItemAsync(WorkspaceSessionContext session, Guid menuItemId, UpdateMenuItemRequestDto request, CancellationToken cancellationToken = default)
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
        var tables = await _context.DiningTables
            .AsNoTracking()
            .Include(item => item.QrCodeAccess)
            .Include(item => item.Orders)
            .Where(item => item.CompanyId == session.CompanyId && item.IsActive)
            .OrderBy(item => item.InternalCode)
            .ToListAsync(cancellationToken);

        return tables.Select(table => MapDiningTable(table)).ToList();
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

        return MapDiningTable(table, qrCodeAccess.PublicCode, qrCodeAccess.AccessPath);
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

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        var safeExtension = extension is ".wav" or ".mp3" or ".ogg"
            ? extension
            : file.ContentType switch
            {
                "audio/mpeg" or "audio/mp3" => ".mp3",
                "audio/ogg" => ".ogg",
                _ => ".wav"
            };

        var webRootPath = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;

        var relativeDirectory = Path.Combine("uploads", "alerts", session.CompanyId.ToString("N"), "tables", table.Id.ToString("N"));
        var directoryPath = Path.Combine(webRootPath, relativeDirectory);
        Directory.CreateDirectory(directoryPath);

        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var filePath = Path.Combine(directoryPath, fileName);

        await using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var relativePath = "/" + Path.Combine(relativeDirectory, fileName).Replace("\\", "/");
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
            ParsePaymentMethodOrDefault(request.PaymentMethod),
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

        if (nextStatus is OrderStatus.Ready or OrderStatus.Delivered)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Password);
            await ValidateOwnerPasswordAsync(session, request.Password, cancellationToken);
        }

        var orders = await _context.CustomerOrders
            .Include(item => item.Items)
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
            .Include(item => item.Items)
            .Include(item => item.DiningTable)
            .FirstOrDefaultAsync(
                item => item.Id == orderId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Pedido nao encontrado.");

        var paymentStatus = ParsePaymentStatus(request.PaymentStatus);

        if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
        {
            order.UpdatePaymentMethod(ParsePaymentMethod(request.PaymentMethod));
        }

        switch (paymentStatus)
        {
            case PaymentStatus.Pending:
                order.MarkPaymentPending();
                break;
            case PaymentStatus.Paid:
                if (order.PaymentMethod == PaymentMethod.Undefined)
                {
                    throw new ArgumentException("Selecione a forma de pagamento no caixa.", nameof(request.PaymentMethod));
                }

                order.MarkPaid();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request.PaymentStatus), "Unsupported payment status.");
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

        if (orders.Any(item => item.Status == OrderStatus.Delivered && item.PaymentStatus != PaymentStatus.Paid))
        {
            throw new InvalidOperationException("Pedidos concluidos sem pagamento precisam permanecer no caixa.");
        }

        if (orders.Any(item => item.PaymentStatus == PaymentStatus.Paid))
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Password);
            await ValidateOwnerPasswordAsync(session, request.Password, cancellationToken);
        }

        await DeleteOrdersAsync(session, orders, "Remocao em lote de pedidos encerrados.", cancellationToken);
    }

    public async Task DeleteTodayOrderFlowAsync(WorkspaceSessionContext session, OwnerPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Password);

        await ValidateOwnerPasswordAsync(session, request.Password, cancellationToken);

        var orders = await _context.CustomerOrders
            .Include(item => item.Items)
            .Include(item => item.DiningTable)
            .Where(item =>
                item.CompanyId == session.CompanyId &&
                item.IsActive)
            .ToListAsync(cancellationToken);

        if (orders.Count == 0)
        {
            return;
        }

        await DeleteOrdersAsync(session, orders, "Limpeza geral da operacao atual.", cancellationToken);
    }

    public async Task<GeneratedWorkspaceFile> GenerateDailyCashReportPdfAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        var timeZone = GetBusinessTimeZone();
        var (dayStartUtc, dayEndUtc, localNow) = GetCurrentBusinessDayRangeUtc(timeZone);

        var activeOrders = await _context.CustomerOrders
            .AsNoTracking()
            .Include(item => item.Items)
            .Include(item => item.DiningTable)
            .Where(item =>
                item.CompanyId == session.CompanyId &&
                item.IsActive &&
                (
                    (item.SubmittedAtUtc >= dayStartUtc && item.SubmittedAtUtc < dayEndUtc) ||
                    (item.PaidAtUtc.HasValue && item.PaidAtUtc.Value >= dayStartUtc && item.PaidAtUtc.Value < dayEndUtc) ||
                    (item.PaymentStatus != PaymentStatus.Paid && item.Status != OrderStatus.Cancelled)
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

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        var safeExtension = extension is ".wav" or ".mp3" or ".ogg"
            ? extension
            : file.ContentType switch
            {
                "audio/mpeg" or "audio/mp3" => ".mp3",
                "audio/ogg" => ".ogg",
                _ => ".wav"
            };

        var webRootPath = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;

        var relativeDirectory = Path.Combine("uploads", "alerts", session.CompanyId.ToString("N"));
        var directoryPath = Path.Combine(webRootPath, relativeDirectory);
        Directory.CreateDirectory(directoryPath);

        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var filePath = Path.Combine(directoryPath, fileName);

        await using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var relativePath = "/" + Path.Combine(relativeDirectory, fileName).Replace("\\", "/");

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
            ParsePaymentMethodOrDefault(request.PaymentMethod),
            cancellationToken);
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

        var waiterCall = await _context.WaiterCalls
            .Include(item => item.DiningTable)
            .FirstOrDefaultAsync(
                item => item.DiningTableId == table.Id &&
                        item.CompanyId == table.CompanyId &&
                        item.IsActive &&
                        item.ResolvedAtUtc == null,
                cancellationToken);

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
        List<OrderItemInputDto> items,
        List<MenuOrderSelectionDto> menuSelections,
        PaymentMethod paymentMethod,
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

        return await PersistOrderAsync(table, customerName, notes, orderItems, paymentMethod, cancellationToken);
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
                .Include(item => item.MenuCategory)
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
                        menuItem.MenuCategory.Name,
                        NormalizeMenuImagePath(menuItem.ImageUrl),
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
                    null,
                    null,
                    item.Notes))
            .ToList();
    }

    private async Task<CustomerOrderDto> PersistOrderAsync(
        DiningTable table,
        string? customerName,
        string? notes,
        List<OrderItem> orderItems,
        PaymentMethod paymentMethod,
        CancellationToken cancellationToken)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(
                item => item.Id == table.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Company not found.");

        var nextNumber = company.ReserveNextOrderNumber();
        var order = new CustomerOrder(
            table.TenantId,
            table.CompanyId,
            table.Id,
            nextNumber,
            customerName,
            notes,
            orderItems,
            paymentMethod);

        if (!company.EnableAutomaticPrinting)
        {
            order.DisablePrinting();
        }

        table.ChangeStatus(TableStatus.Occupied);

        await _context.CustomerOrders.AddAsync(order, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapOrder(order, table.Name);
    }

    private async Task ValidateOwnerPasswordAsync(
        WorkspaceSessionContext session,
        string password,
        CancellationToken cancellationToken)
    {
        var owner = await _context.Users
            .FirstOrDefaultAsync(
                user => user.CompanyId == session.CompanyId &&
                        user.Role == UserRole.Owner &&
                        user.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Owner nao encontrado.");

        if (!_passwordHasher.Verify(password, owner.PasswordHash))
        {
            throw new InvalidOperationException("Senha do owner incorreta.");
        }
    }

    private async Task ApplyOrderStatusAsync(CustomerOrder order, OrderStatus nextStatus, CancellationToken cancellationToken)
    {
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
                    Label = "A cobrar agora",
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
                ItemsLabel = BuildItemsSummary(item.Items.Select(orderItem => (orderItem.Quantity, orderItem.Name))),
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
                ItemsLabel = BuildItemsSummary(item.Items.Select(orderItem => (orderItem.Quantity, orderItem.Name))),
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
            order.Notes,
            BuildItemsSummary(order.Items.Select(item => (item.Quantity, item.Name))),
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
        var summary = string.Join(
            " | ",
            items.Select(item => $"{item.Quantity.ToString("0.##", PtBrCulture)}x {item.Name}".Trim()));

        if (summary.Length <= 3900)
        {
            return summary;
        }

        return $"{summary[..3897]}...";
    }

    private static string? BuildActiveOrderNotes(CustomerOrder order, TimeZoneInfo timeZone)
    {
        var notes = new List<string>();

        if (!string.IsNullOrWhiteSpace(order.Notes))
        {
            notes.Add(order.Notes.Trim());
        }

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
            PaymentMethod.Undefined => "A definir",
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

    private static CustomerOrderDto MapOrder(CustomerOrder order, string? tableName = null)
    {
        return new CustomerOrderDto
        {
            Id = order.Id,
            Number = order.Number,
            TableId = order.DiningTableId,
            TableName = tableName ?? order.DiningTable.Name,
            Status = order.Status.ToString(),
            PaymentMethod = order.PaymentMethod.ToString(),
            RequestedPaymentMethod = order.RequestedPaymentMethod.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            PrintStatus = order.PrintStatus.ToString(),
            CustomerName = order.CustomerName,
            Notes = order.Notes,
            TotalAmount = order.TotalAmount,
            SubmittedAtUtc = order.SubmittedAtUtc,
            PaidAtUtc = order.PaidAtUtc,
            PrintedAtUtc = order.PrintedAtUtc,
            PrintAttempts = order.PrintAttempts,
            PrintLastError = order.PrintLastError,
            PrintAgentName = order.PrintAgentName,
            PrintPrinterName = order.PrintPrinterName,
            Items = order.Items.Select(item => new OrderItemDto
            {
                Id = item.Id,
                Name = item.Name,
                CategoryName = item.CategoryName,
                ImageUrl = NormalizeMenuImagePath(item.ImageUrl),
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
            ContactPhone = company.ContactPhone,
            Alerts = MapAlertSettings(company)
        };
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
            DisplayOrder = item.DisplayOrder,
            IsActive = item.IsActive
        };
    }

    private static string? NormalizeMenuImagePath(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        var normalized = imageUrl.Trim();

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
}
