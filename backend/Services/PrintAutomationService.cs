using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.Domain.Plans;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class PrintAutomationService : IPrintAutomationService
{
    private static readonly TimeSpan AgentOnlineThreshold = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan ClaimTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan HeartbeatWriteThreshold = TimeSpan.FromSeconds(10);
    private const string AgentDownloadVersion = "20260503-compat-paper";
    private const string DownloadUrlX86 = "/downloads/zeropaper-print-agent-win-x86.exe?v=" + AgentDownloadVersion;
    private const string DownloadUrlX64 = "/downloads/zeropaper-print-agent-win-x64.exe?v=" + AgentDownloadVersion;
    private const string LegacyDownloadUrl = "/downloads/zeropaper-print-agent-legacy-net48.zip?v=" + AgentDownloadVersion;
    private const string TestJobTableName = "Teste de impressao";

    private readonly ZeroPaperDbContext _context;
    private readonly ILogger<PrintAutomationService> _logger;

    public PrintAutomationService(ZeroPaperDbContext context, ILogger<PrintAutomationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PrintingSettingsDto> GetPrintingSettingsAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == session.CompanyId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");

        var agent = await _context.PrintAgents
            .AsNoTracking()
            .Where(item => item.CompanyId == session.CompanyId && item.IsActive)
            .OrderByDescending(item => item.LastSeenAtUtc ?? item.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

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
                CustomerName = item.CustomerName,
                IsDeliveryOrder = item.DiningTable.IsDeliveryChannel,
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

        var printJobCounters = await _context.PrintJobs
            .AsNoTracking()
            .Where(item => item.CompanyId == session.CompanyId && item.IsActive)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                PendingJobs = group.Count(item => item.Status == PrintStatus.Pending || item.Status == PrintStatus.Processing),
                FailedJobs = group.Count(item => item.Status == PrintStatus.Failed),
                PrintedJobs = group.Count(item => item.Status == PrintStatus.Printed)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return BuildPrintingSettings(
            company,
            agent,
            recentOrders,
            (counters?.PendingJobs ?? 0) + (printJobCounters?.PendingJobs ?? 0),
            (counters?.FailedJobs ?? 0) + (printJobCounters?.FailedJobs ?? 0),
            (counters?.PrintedJobs ?? 0) + (printJobCounters?.PrintedJobs ?? 0),
            session.HasAutoPrint);
    }

    public async Task<PrintingSettingsDto> UpdatePrintingSettingsAsync(WorkspaceSessionContext session, UpdatePrintingSettingsRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var company = await _context.Companies
            .FirstOrDefaultAsync(item => item.Id == session.CompanyId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");

        var paperProfile = ParsePaperProfile(request.PaperProfile);
        var ordersPerPage = NormalizeOrdersPerPage(paperProfile, request.OrdersPerPage);
        var enableAutomaticPrinting = request.EnableAutomaticPrinting;

        if (enableAutomaticPrinting && !session.HasAutoPrint)
        {
            throw new InvalidOperationException("A impressao automatica nao faz parte do plano atual da unidade.");
        }

        company.UpdatePrintingPreferences(enableAutomaticPrinting, paperProfile, ordersPerPage);

        if (!enableAutomaticPrinting)
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
        var keyHash = ComputeKeyHash(rawKey);
        company.RotatePrintAgentKey(keyHash);
        await UpsertPrimaryAgentTokenAsync(company, keyHash, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "SecurityAudit action={Action} company={CompanyId} user={UserId}",
            "rotate-print-agent-key",
            session.CompanyId,
            session.UserId);

        return new RotatePrintingAgentKeyResponseDto
        {
            AgentKey = rawKey,
            AgentToken = rawKey,
            Printing = await GetPrintingSettingsAsync(session, cancellationToken)
        };
    }

    public async Task<PrintTestJobResponseDto> CreateTestJobAsync(WorkspaceSessionContext session, CreatePrintTestJobRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var company = await _context.Companies
            .FirstOrDefaultAsync(item => item.Id == session.CompanyId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");

        var job = PrintJob.CreateTest(company.TenantId, company.Id, request.Notes);
        _context.PrintJobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "SecurityAudit action={Action} company={CompanyId} user={UserId} job={JobId}",
            "create-print-test-job",
            session.CompanyId,
            session.UserId,
            job.Id);

        return new PrintTestJobResponseDto
        {
            JobId = job.Id,
            Status = job.Status.ToString(),
            QueuedAtUtc = job.QueuedAtUtc,
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
        _logger.LogInformation(
            "SecurityAudit action={Action} company={CompanyId} user={UserId} order={OrderId}",
            "requeue-print-order",
            session.CompanyId,
            session.UserId,
            orderId);
    }

    public async Task RegisterAgentHeartbeatAsync(string agentKey, PrintAgentHeartbeatRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentKey);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AgentName);

        var context = await ResolveAgentContextByKeyAsync(agentKey, cancellationToken);
        if (RefreshHeartbeatIfNeeded(context.Company, context.Agent, request.AgentName, request.PrinterName, request.AppVersion, DateTime.UtcNow))
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<PrintAgentRegistrationResponseDto> RegisterAgentAsync(string agentKey, PrintAgentHeartbeatRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentKey);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AgentName);

        var context = await ResolveAgentContextByKeyAsync(agentKey, cancellationToken);
        var agent = context.Agent ?? await UpsertPrimaryAgentTokenAsync(context.Company, ComputeKeyHash(agentKey), cancellationToken);
        var utcNow = DateTime.UtcNow;

        agent.Register(request.AgentName, request.PrinterName, request.AppVersion, utcNow);
        context.Company.RegisterPrintAgentHeartbeat(request.AgentName, request.PrinterName, utcNow);
        await _context.SaveChangesAsync(cancellationToken);

        return new PrintAgentRegistrationResponseDto
        {
            AgentId = agent.Id,
            CompanyId = context.Company.Id,
            AutoPrintEnabled = context.Company.EnableAutomaticPrinting &&
                               await CanAutoPrintAsync(context.Company.TenantId, cancellationToken),
            PaperProfile = context.Company.PrintPaperProfile.ToString(),
            OrdersPerPage = context.Company.PrintOrdersPerPage,
            RegisteredAtUtc = agent.RegisteredAtUtc ?? utcNow
        };
    }

    public async Task<PrintAgentOrderJobDto?> ClaimNextOrderJobAsync(string agentKey, PrintAgentClaimRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentKey);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AgentName);

        var context = await ResolveAgentContextByKeyAsync(agentKey, cancellationToken);
        var company = context.Company;
        var utcNow = DateTime.UtcNow;
        var heartbeatTouched = RefreshHeartbeatIfNeeded(company, context.Agent, request.AgentName, request.PrinterName, null, utcNow);

        var canAutoPrint = await CanAutoPrintAsync(company.TenantId, cancellationToken);

        if (!company.EnableAutomaticPrinting || !canAutoPrint)
        {
            if (heartbeatTouched)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return null;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

        var staleClaimThreshold = utcNow.Subtract(ClaimTimeout);

        var printJob = await _context.PrintJobs
            .Include(item => item.Company)
            .Where(item =>
                item.CompanyId == company.Id &&
                item.IsActive &&
                (item.Status == PrintStatus.Pending ||
                 (item.Status == PrintStatus.Processing && item.ClaimedAtUtc != null && item.ClaimedAtUtc < staleClaimThreshold)))
            .OrderBy(item => item.QueuedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (printJob is not null)
        {
            printJob.Claim(request.AgentName, request.PrinterName);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return BuildPrintJobDto(company, printJob);
        }

        var order = await _context.CustomerOrders
            .Include(item => item.Items)
                .ThenInclude(item => item.AdditionalSelections)
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
            JobId = order.Id,
            JobKind = "Order",
            IsTest = false,
            Number = order.Number,
            PaperProfile = company.PrintPaperProfile.ToString(),
            OrdersPerPage = company.PrintOrdersPerPage,
            RestaurantName = order.Company.TradeName,
            TableName = order.DiningTable.Name,
            CustomerName = order.CustomerName,
            DeliveryPhone = order.DeliveryPhone,
            DeliveryAddress = order.DeliveryAddress,
            DeliveryNumber = order.DeliveryNumber,
            DeliveryComplement = order.DeliveryComplement,
            DeliveryPostalCode = order.DeliveryPostalCode,
            DeliveryFreightAmount = order.DeliveryFreightAmount,
            DeliveryDistanceKm = order.DeliveryDistanceKm,
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
                    BaseUnitPrice = item.BaseUnitPrice,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice,
                    Notes = item.Notes,
                    Additionals = item.AdditionalSelections
                        .OrderBy(selection => selection.GroupName)
                        .ThenBy(selection => selection.OptionName)
                        .Select(selection => new PrintAgentOrderAdditionalDto
                        {
                            GroupName = selection.GroupName,
                            OptionName = selection.OptionName,
                            UnitPrice = selection.UnitPrice
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    public async Task CompleteOrderJobAsync(string agentKey, Guid orderId, CompletePrintJobRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentKey);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AgentName);

        var context = await ResolveAgentContextByKeyAsync(agentKey, cancellationToken);
        var company = context.Company;
        RefreshHeartbeatIfNeeded(company, context.Agent, request.AgentName, request.PrinterName, null, DateTime.UtcNow);

        var order = await _context.CustomerOrders
            .FirstOrDefaultAsync(
                item => item.Id == orderId &&
                        item.CompanyId == company.Id &&
                        item.IsActive,
                cancellationToken);

        if (order is not null)
        {
            order.MarkPrinted(request.AgentName, request.PrinterName);
        }
        else
        {
            var printJob = await _context.PrintJobs
                .FirstOrDefaultAsync(
                    item => item.Id == orderId &&
                            item.CompanyId == company.Id &&
                            item.IsActive,
                    cancellationToken)
                ?? throw new KeyNotFoundException("Job nao encontrado para impressao.");

            printJob.MarkPrinted(request.AgentName, request.PrinterName);
            context.Agent?.ClearError();
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteOrderBatchAsync(string agentKey, CompletePrintJobBatchRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentKey);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AgentName);

        var context = await ResolveAgentContextByKeyAsync(agentKey, cancellationToken);
        var company = context.Company;
        RefreshHeartbeatIfNeeded(company, context.Agent, request.AgentName, request.PrinterName, null, DateTime.UtcNow);

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

        var foundIds = orders.Select(item => item.Id).ToHashSet();
        var missingIds = orderIds.Where(item => !foundIds.Contains(item)).ToList();
        var printJobs = missingIds.Count == 0
            ? new List<PrintJob>()
            : await _context.PrintJobs
                .Where(item =>
                    missingIds.Contains(item.Id) &&
                    item.CompanyId == company.Id &&
                    item.IsActive)
                .ToListAsync(cancellationToken);

        if (orders.Count + printJobs.Count != orderIds.Count)
        {
            throw new KeyNotFoundException("Nem todos os jobs informados pertencem a esta unidade.");
        }

        foreach (var order in orders)
        {
            order.MarkPrinted(request.AgentName, request.PrinterName);
        }

        foreach (var printJob in printJobs)
        {
            printJob.MarkPrinted(request.AgentName, request.PrinterName);
        }

        context.Agent?.ClearError();
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task FailOrderJobAsync(string agentKey, Guid orderId, FailPrintJobRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentKey);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AgentName);

        var context = await ResolveAgentContextByKeyAsync(agentKey, cancellationToken);
        var company = context.Company;
        var utcNow = DateTime.UtcNow;
        RefreshHeartbeatIfNeeded(company, context.Agent, request.AgentName, request.PrinterName, null, utcNow);

        var order = await _context.CustomerOrders
            .FirstOrDefaultAsync(
                item => item.Id == orderId &&
                        item.CompanyId == company.Id &&
                        item.IsActive,
                cancellationToken);

        if (order is not null)
        {
            order.MarkPrintFailed(request.ErrorMessage, request.AgentName, request.PrinterName);
        }
        else
        {
            var printJob = await _context.PrintJobs
                .FirstOrDefaultAsync(
                    item => item.Id == orderId &&
                            item.CompanyId == company.Id &&
                            item.IsActive,
                    cancellationToken)
                ?? throw new KeyNotFoundException("Job nao encontrado para impressao.");

            printJob.MarkFailed(request.ErrorMessage, request.AgentName, request.PrinterName);
        }

        context.Agent?.RegisterError(request.ErrorMessage, utcNow);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task FailOrderBatchAsync(string agentKey, FailPrintJobBatchRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentKey);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AgentName);

        var context = await ResolveAgentContextByKeyAsync(agentKey, cancellationToken);
        var company = context.Company;
        var utcNow = DateTime.UtcNow;
        RefreshHeartbeatIfNeeded(company, context.Agent, request.AgentName, request.PrinterName, null, utcNow);

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

        var foundIds = orders.Select(item => item.Id).ToHashSet();
        var missingIds = orderIds.Where(item => !foundIds.Contains(item)).ToList();
        var printJobs = missingIds.Count == 0
            ? new List<PrintJob>()
            : await _context.PrintJobs
                .Where(item =>
                    missingIds.Contains(item.Id) &&
                    item.CompanyId == company.Id &&
                    item.IsActive)
                .ToListAsync(cancellationToken);

        if (orders.Count + printJobs.Count != orderIds.Count)
        {
            throw new KeyNotFoundException("Nem todos os jobs informados pertencem a esta unidade.");
        }

        foreach (var order in orders)
        {
            order.MarkPrintFailed(request.ErrorMessage, request.AgentName, request.PrinterName);
        }

        foreach (var printJob in printJobs)
        {
            printJob.MarkFailed(request.ErrorMessage, request.AgentName, request.PrinterName);
        }

        context.Agent?.RegisterError(request.ErrorMessage, utcNow);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<PrintAgentContext> ResolveAgentContextByKeyAsync(string rawKey, CancellationToken cancellationToken)
    {
        var keyHash = ComputeKeyHash(rawKey);

        var agent = await _context.PrintAgents
            .Include(item => item.Company)
            .FirstOrDefaultAsync(
                item => item.TokenHash == keyHash &&
                        item.IsActive &&
                        item.Company.IsActive,
                cancellationToken);

        if (agent is not null)
        {
            return new PrintAgentContext(agent.Company, agent);
        }

        var company = await _context.Companies
            .FirstOrDefaultAsync(
                item => item.PrintAgentKeyHash == keyHash &&
                        item.IsActive,
                cancellationToken)
            ?? throw new UnauthorizedAccessException("Agente de impressao nao autorizado.");

        agent = await UpsertPrimaryAgentTokenAsync(company, keyHash, cancellationToken);
        return new PrintAgentContext(company, agent);
    }

    private async Task<PrintAgent> UpsertPrimaryAgentTokenAsync(Company company, string keyHash, CancellationToken cancellationToken)
    {
        var agent = await _context.PrintAgents
            .FirstOrDefaultAsync(item => item.CompanyId == company.Id && item.IsActive, cancellationToken);

        if (agent is null)
        {
            agent = new PrintAgent(company.TenantId, company.Id, keyHash);
            _context.PrintAgents.Add(agent);
            return agent;
        }

        agent.RotateToken(keyHash);
        return agent;
    }

    private PrintingSettingsDto BuildPrintingSettings(
        Company company,
        PrintAgent? agent,
        IReadOnlyList<PrintOrderSummaryDto> recentOrders,
        int pendingJobs,
        int failedJobs,
        int printedJobs,
        bool canAutoPrint)
    {
        var utcNow = DateTime.UtcNow;
        var lastSeenAtUtc = agent?.LastSeenAtUtc ?? company.PrintAgentLastSeenAtUtc;
        var autoPrintEnabled = company.EnableAutomaticPrinting && canAutoPrint;

        return new PrintingSettingsDto
        {
            EnableAutomaticPrinting = autoPrintEnabled,
            AutoPrintEnabled = autoPrintEnabled,
            CanAutoPrint = canAutoPrint,
            PaperProfile = company.PrintPaperProfile.ToString(),
            OrdersPerPage = company.PrintOrdersPerPage,
            HasAgentKey = !string.IsNullOrWhiteSpace(company.PrintAgentKeyHash) || agent is not null,
            HasAgentToken = !string.IsNullOrWhiteSpace(company.PrintAgentKeyHash) || agent is not null,
            AgentId = agent?.Id,
            AgentOnline = lastSeenAtUtc.HasValue && lastSeenAtUtc.Value >= utcNow.Subtract(AgentOnlineThreshold),
            AgentName = agent?.Name ?? company.PrintAgentName,
            PrinterName = agent?.PrinterName ?? company.PrintAgentPrinterName,
            AppVersion = agent?.AppVersion,
            RegisteredAtUtc = agent?.RegisteredAtUtc,
            LastSeenAtUtc = lastSeenAtUtc,
            LastError = agent?.LastError,
            LastErrorAtUtc = agent?.LastErrorAtUtc,
            PendingJobs = pendingJobs,
            FailedJobs = failedJobs,
            PrintedJobs = printedJobs,
            DownloadUrl = DownloadUrlX86,
            DownloadUrlX86 = DownloadUrlX86,
            DownloadUrlX64 = DownloadUrlX64,
            LegacyDownloadUrl = LegacyDownloadUrl,
            RecentOrders = recentOrders.ToList()
        };
    }

    private async Task<bool> CanAutoPrintAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var planName = await _context.Subscriptions
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId &&
                           item.IsActive &&
                           (item.Status == SubscriptionStatus.Active || item.Status == SubscriptionStatus.Trial))
            .OrderByDescending(item => item.StartsAtUtc)
            .Select(item => item.PlanName)
            .FirstOrDefaultAsync(cancellationToken);

        return CommercialPlanCatalog.ResolveFeatures(planName).HasAutoPrint;
    }

    private PrintAgentOrderJobDto BuildPrintJobDto(Company company, PrintJob job)
    {
        return new PrintAgentOrderJobDto
        {
            OrderId = job.Id,
            JobId = job.Id,
            JobKind = job.Kind.ToString(),
            IsTest = job.Kind == PrintJobKind.Test,
            Number = 0,
            PaperProfile = company.PrintPaperProfile.ToString(),
            OrdersPerPage = company.PrintOrdersPerPage,
            RestaurantName = company.TradeName,
            TableName = TestJobTableName,
            CustomerName = "ZeroPaper",
            Notes = job.Notes,
            PaymentMethod = "Teste",
            ContactPhone = company.ContactPhone,
            SubmittedAtUtc = job.QueuedAtUtc,
            TotalAmount = 0,
            Items =
            [
                new PrintAgentOrderItemDto
                {
                    Name = job.Title,
                    CategoryName = "Teste",
                    Quantity = 1,
                    BaseUnitPrice = 0,
                    UnitPrice = 0,
                    TotalPrice = 0,
                    Notes = "Se esta via saiu corretamente, o agente Windows esta conectado."
                }
            ]
        };
    }

    private static string ComputeKeyHash(string rawKey)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(rawKey.Trim());
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToHexString(hashBytes);
    }

    private static bool RefreshHeartbeatIfNeeded(Company company, PrintAgent? agent, string agentName, string? printerName, string? appVersion, DateTime utcNow)
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
            if (agent is null)
            {
                return false;
            }

            agent.UpdateHeartbeat(normalizedAgentName, normalizedPrinterName, appVersion, utcNow);
            return true;
        }

        company.RegisterPrintAgentHeartbeat(normalizedAgentName, normalizedPrinterName, utcNow);
        agent?.UpdateHeartbeat(normalizedAgentName, normalizedPrinterName, appVersion, utcNow);
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

    private sealed record PrintAgentContext(Company Company, PrintAgent? Agent);
}
