using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class PrintAutomationService : IPrintAutomationService
{
    private static readonly TimeSpan AgentOnlineThreshold = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan ClaimTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan HeartbeatWriteThreshold = TimeSpan.FromSeconds(10);
    private const string DownloadUrl = "/downloads/zeropaper-print-agent-win-x64.exe";

    private readonly ZeroPaperDbContext _context;

    public PrintAutomationService(ZeroPaperDbContext context)
    {
        _context = context;
    }

    public async Task<PrintingSettingsDto> GetPrintingSettingsAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == session.CompanyId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");

        var recentOrders = await _context.CustomerOrders
            .AsNoTracking()
            .Where(item => item.CompanyId == session.CompanyId && item.IsActive && item.PrintStatus != PrintStatus.Disabled)
            .OrderByDescending(item => item.SubmittedAtUtc)
            .Take(10)
            .Select(item => new PrintOrderSummaryDto
            {
                Id = item.Id,
                Number = item.Number,
                TableName = item.DiningTable.Name,
                Status = item.Status.ToString(),
                PrintStatus = item.PrintStatus.ToString(),
                TotalAmount = item.TotalAmount,
                SubmittedAtUtc = item.SubmittedAtUtc,
                PrintedAtUtc = item.PrintedAtUtc,
                PrintAttempts = item.PrintAttempts,
                PrintLastError = item.PrintLastError
            })
            .ToListAsync(cancellationToken);

        var counters = await _context.CustomerOrders
            .AsNoTracking()
            .Where(item => item.CompanyId == session.CompanyId && item.IsActive)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                PendingJobs = group.Count(item =>
                    item.Status != OrderStatus.Cancelled &&
                    (item.PrintStatus == PrintStatus.Pending || item.PrintStatus == PrintStatus.Processing)),
                FailedJobs = group.Count(item => item.PrintStatus == PrintStatus.Failed),
                PrintedJobs = group.Count(item => item.PrintStatus == PrintStatus.Printed)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return BuildPrintingSettings(
            company,
            recentOrders,
            counters?.PendingJobs ?? 0,
            counters?.FailedJobs ?? 0,
            counters?.PrintedJobs ?? 0);
    }

    public async Task<PrintingSettingsDto> UpdatePrintingSettingsAsync(WorkspaceSessionContext session, UpdatePrintingSettingsRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var company = await _context.Companies
            .FirstOrDefaultAsync(item => item.Id == session.CompanyId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");

        var paperProfile = ParsePaperProfile(request.PaperProfile);
        var ordersPerPage = NormalizeOrdersPerPage(paperProfile, request.OrdersPerPage);

        company.UpdatePrintingPreferences(request.EnableAutomaticPrinting, paperProfile, ordersPerPage);

        if (!request.EnableAutomaticPrinting)
        {
            var queuedOrders = await _context.CustomerOrders
                .Where(item =>
                    item.CompanyId == session.CompanyId &&
                    item.IsActive &&
                    (item.PrintStatus == PrintStatus.Pending || item.PrintStatus == PrintStatus.Processing))
                .ToListAsync(cancellationToken);

            foreach (var order in queuedOrders)
            {
                order.DisablePrinting();
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return await GetPrintingSettingsAsync(session, cancellationToken);
    }

    public async Task<RotatePrintingAgentKeyResponseDto> RotatePrintingAgentKeyAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(item => item.Id == session.CompanyId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");

        var rawKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        company.RotatePrintAgentKey(ComputeKeyHash(rawKey));
        await _context.SaveChangesAsync(cancellationToken);

        return new RotatePrintingAgentKeyResponseDto
        {
            AgentKey = rawKey,
            Printing = await GetPrintingSettingsAsync(session, cancellationToken)
        };
    }

    public async Task RequeueOrderPrintAsync(WorkspaceSessionContext session, Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _context.CustomerOrders
            .FirstOrDefaultAsync(
                item => item.Id == orderId &&
                        item.CompanyId == session.CompanyId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Pedido nao encontrado.");

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Pedidos cancelados nao voltam para a fila de impressao.");
        }

        order.RequeuePrinting();
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RegisterAgentHeartbeatAsync(string agentKey, PrintAgentHeartbeatRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentKey);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AgentName);

        var company = await ResolveCompanyByAgentKeyAsync(agentKey, cancellationToken);
        if (RefreshHeartbeatIfNeeded(company, request.AgentName, request.PrinterName, DateTime.UtcNow))
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<PrintAgentOrderJobDto?> ClaimNextOrderJobAsync(string agentKey, PrintAgentClaimRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentKey);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AgentName);

        var company = await ResolveCompanyByAgentKeyAsync(agentKey, cancellationToken);
        var utcNow = DateTime.UtcNow;
        var heartbeatTouched = RefreshHeartbeatIfNeeded(company, request.AgentName, request.PrinterName, utcNow);

        if (!company.EnableAutomaticPrinting)
        {
            if (heartbeatTouched)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return null;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

        var staleClaimThreshold = utcNow.Subtract(ClaimTimeout);

        var order = await _context.CustomerOrders
            .Include(item => item.Items)
            .Include(item => item.DiningTable)
            .Include(item => item.Company)
            .Where(item =>
                item.CompanyId == company.Id &&
                item.IsActive &&
                item.Status != OrderStatus.Cancelled &&
                (item.PrintStatus == PrintStatus.Pending ||
                 (item.PrintStatus == PrintStatus.Processing && item.PrintClaimedAtUtc != null && item.PrintClaimedAtUtc < staleClaimThreshold)))
            .OrderBy(item => item.SubmittedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
        {
            await transaction.CommitAsync(cancellationToken);

            if (heartbeatTouched)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return null;
        }

        order.ClaimPrinting(request.AgentName, request.PrinterName);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new PrintAgentOrderJobDto
        {
            OrderId = order.Id,
            Number = order.Number,
            PaperProfile = company.PrintPaperProfile.ToString(),
            OrdersPerPage = company.PrintOrdersPerPage,
            RestaurantName = order.Company.TradeName,
            TableName = order.DiningTable.Name,
            CustomerName = order.CustomerName,
            Notes = order.Notes,
            PaymentMethod = order.PaymentMethod.ToString(),
            ContactPhone = order.Company.ContactPhone,
            SubmittedAtUtc = order.SubmittedAtUtc,
            TotalAmount = order.TotalAmount,
            Items = order.Items
                .OrderBy(item => item.Name)
                .Select(item => new PrintAgentOrderItemDto
                {
                    Name = item.Name,
                    CategoryName = item.CategoryName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice,
                    Notes = item.Notes
                })
                .ToList()
        };
    }

    public async Task CompleteOrderJobAsync(string agentKey, Guid orderId, CompletePrintJobRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentKey);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AgentName);

        var company = await ResolveCompanyByAgentKeyAsync(agentKey, cancellationToken);
        company.RegisterPrintAgentHeartbeat(request.AgentName, request.PrinterName, DateTime.UtcNow);

        var order = await _context.CustomerOrders
            .FirstOrDefaultAsync(
                item => item.Id == orderId &&
                        item.CompanyId == company.Id &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Pedido nao encontrado para impressao.");

        order.MarkPrinted(request.AgentName, request.PrinterName);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteOrderBatchAsync(string agentKey, CompletePrintJobBatchRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentKey);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AgentName);

        var company = await ResolveCompanyByAgentKeyAsync(agentKey, cancellationToken);
        company.RegisterPrintAgentHeartbeat(request.AgentName, request.PrinterName, DateTime.UtcNow);

        var orderIds = request.OrderIds
            .Where(item => item != Guid.Empty)
            .Distinct()
            .ToList();

        if (orderIds.Count == 0)
        {
            throw new InvalidOperationException("Nenhum pedido foi informado para concluir a impressao.");
        }

        var orders = await _context.CustomerOrders
            .Where(item =>
                orderIds.Contains(item.Id) &&
                item.CompanyId == company.Id &&
                item.IsActive)
            .ToListAsync(cancellationToken);

        if (orders.Count != orderIds.Count)
        {
            throw new KeyNotFoundException("Nem todos os pedidos informados pertencem a esta unidade.");
        }

        foreach (var order in orders)
        {
            order.MarkPrinted(request.AgentName, request.PrinterName);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task FailOrderJobAsync(string agentKey, Guid orderId, FailPrintJobRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentKey);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AgentName);

        var company = await ResolveCompanyByAgentKeyAsync(agentKey, cancellationToken);
        company.RegisterPrintAgentHeartbeat(request.AgentName, request.PrinterName, DateTime.UtcNow);

        var order = await _context.CustomerOrders
            .FirstOrDefaultAsync(
                item => item.Id == orderId &&
                        item.CompanyId == company.Id &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Pedido nao encontrado para impressao.");

        order.MarkPrintFailed(request.ErrorMessage, request.AgentName, request.PrinterName);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task FailOrderBatchAsync(string agentKey, FailPrintJobBatchRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentKey);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AgentName);

        var company = await ResolveCompanyByAgentKeyAsync(agentKey, cancellationToken);
        company.RegisterPrintAgentHeartbeat(request.AgentName, request.PrinterName, DateTime.UtcNow);

        var orderIds = request.OrderIds
            .Where(item => item != Guid.Empty)
            .Distinct()
            .ToList();

        if (orderIds.Count == 0)
        {
            throw new InvalidOperationException("Nenhum pedido foi informado para registrar falha.");
        }

        var orders = await _context.CustomerOrders
            .Where(item =>
                orderIds.Contains(item.Id) &&
                item.CompanyId == company.Id &&
                item.IsActive)
            .ToListAsync(cancellationToken);

        if (orders.Count != orderIds.Count)
        {
            throw new KeyNotFoundException("Nem todos os pedidos informados pertencem a esta unidade.");
        }

        foreach (var order in orders)
        {
            order.MarkPrintFailed(request.ErrorMessage, request.AgentName, request.PrinterName);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Company> ResolveCompanyByAgentKeyAsync(string rawKey, CancellationToken cancellationToken)
    {
        var keyHash = ComputeKeyHash(rawKey);

        return await _context.Companies
            .FirstOrDefaultAsync(
                item => item.PrintAgentKeyHash == keyHash &&
                        item.IsActive,
                cancellationToken)
            ?? throw new UnauthorizedAccessException("Agente de impressao nao autorizado.");
    }

    private PrintingSettingsDto BuildPrintingSettings(Company company, IReadOnlyList<PrintOrderSummaryDto> recentOrders, int pendingJobs, int failedJobs, int printedJobs)
    {
        var utcNow = DateTime.UtcNow;

        return new PrintingSettingsDto
        {
            EnableAutomaticPrinting = company.EnableAutomaticPrinting,
            PaperProfile = company.PrintPaperProfile.ToString(),
            OrdersPerPage = company.PrintOrdersPerPage,
            HasAgentKey = !string.IsNullOrWhiteSpace(company.PrintAgentKeyHash),
            AgentOnline = company.PrintAgentLastSeenAtUtc.HasValue && company.PrintAgentLastSeenAtUtc.Value >= utcNow.Subtract(AgentOnlineThreshold),
            AgentName = company.PrintAgentName,
            PrinterName = company.PrintAgentPrinterName,
            LastSeenAtUtc = company.PrintAgentLastSeenAtUtc,
            PendingJobs = pendingJobs,
            FailedJobs = failedJobs,
            PrintedJobs = printedJobs,
            DownloadUrl = DownloadUrl,
            RecentOrders = recentOrders.ToList()
        };
    }

    private static string ComputeKeyHash(string rawKey)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(rawKey.Trim());
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToHexString(hashBytes);
    }

    private static bool RefreshHeartbeatIfNeeded(Company company, string agentName, string? printerName, DateTime utcNow)
    {
        var normalizedAgentName = agentName.Trim();
        var normalizedPrinterName = string.IsNullOrWhiteSpace(printerName) ? null : printerName.Trim();

        var requiresUpdate =
            !string.Equals(company.PrintAgentName, normalizedAgentName, StringComparison.Ordinal) ||
            !string.Equals(company.PrintAgentPrinterName, normalizedPrinterName, StringComparison.Ordinal) ||
            !company.PrintAgentLastSeenAtUtc.HasValue ||
            company.PrintAgentLastSeenAtUtc.Value <= utcNow.Subtract(HeartbeatWriteThreshold);

        if (!requiresUpdate)
        {
            return false;
        }

        company.RegisterPrintAgentHeartbeat(normalizedAgentName, normalizedPrinterName, utcNow);
        return true;
    }

    private static PrintPaperProfile ParsePaperProfile(string? value)
    {
        if (Enum.TryParse<PrintPaperProfile>(value, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return PrintPaperProfile.Thermal80mm;
    }

    private static int NormalizeOrdersPerPage(PrintPaperProfile paperProfile, int ordersPerPage)
    {
        if (paperProfile == PrintPaperProfile.Thermal80mm)
        {
            return 1;
        }

        return ordersPerPage is 2 or 4 ? ordersPerPage : 1;
    }
}
