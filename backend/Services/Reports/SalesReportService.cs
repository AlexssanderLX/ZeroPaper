using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Workspace.Reports;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Reports;

public sealed class SalesReportService : ISalesReportService
{
    private const string DefaultTimeZoneId = "America/Sao_Paulo";
    private readonly ZeroPaperDbContext _context;

    public SalesReportService(ZeroPaperDbContext context)
    {
        _context = context;
    }

    public async Task<DailySalesReportDto> GetDailySalesReportAsync(
        WorkspaceSessionContext session,
        DateOnly referenceDate,
        CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == session.CompanyId &&
                        item.TenantId == session.TenantId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");

        var timeZone = ResolveTimeZone(company.TimeZoneId);
        var startLocal = DateTime.SpecifyKind(referenceDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var endLocal = DateTime.SpecifyKind(referenceDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, timeZone);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, timeZone);

        var liveOrders = await _context.CustomerOrders
            .AsNoTracking()
            .Include(item => item.Payments)
            .Where(item =>
                item.CompanyId == session.CompanyId &&
                item.SubmittedAtUtc >= startUtc &&
                item.SubmittedAtUtc < endUtc)
            .ToListAsync(cancellationToken);

        var deletedOrders = await _context.DeletedOrderRecords
            .AsNoTracking()
            .Where(item =>
                item.CompanyId == session.CompanyId &&
                item.SubmittedAtUtc >= startUtc &&
                item.SubmittedAtUtc < endUtc)
            .ToListAsync(cancellationToken);

        var liveCancelledOrders = liveOrders
            .Where(item => item.Status == OrderStatus.Cancelled)
            .ToList();
        var deletedCancelledOrders = deletedOrders
            .Where(item => item.Status == OrderStatus.Cancelled)
            .ToList();

        var liveNonCancelledOrders = liveOrders
            .Where(item => item.Status != OrderStatus.Cancelled)
            .ToList();
        var deletedNonCancelledOrders = deletedOrders
            .Where(item => item.Status != OrderStatus.Cancelled)
            .ToList();

        var livePaidOrders = liveNonCancelledOrders
            .Where(item => item.PaymentStatus == PaymentStatus.Paid)
            .ToList();
        var deletedPaidOrders = deletedNonCancelledOrders
            .Where(item => item.PaymentStatus == PaymentStatus.Paid)
            .ToList();

        var livePendingOrders = liveNonCancelledOrders
            .Where(item => item.PaymentStatus != PaymentStatus.Paid)
            .ToList();
        var deletedPendingOrders = deletedNonCancelledOrders
            .Where(item => item.PaymentStatus != PaymentStatus.Paid)
            .ToList();

        var ordersSubmittedCount = liveOrders.Count + deletedOrders.Count;
        var paidOrdersCount = livePaidOrders.Count + deletedPaidOrders.Count;
        var pendingOrdersCount = livePendingOrders.Count + deletedPendingOrders.Count;
        var cancelledOrdersCount = liveCancelledOrders.Count + deletedCancelledOrders.Count;
        var totalSalesAmount = SumTotals(liveNonCancelledOrders) + SumTotals(deletedNonCancelledOrders);
        var paidAmount = SumTotals(livePaidOrders) + SumTotals(deletedPaidOrders);
        var pendingAmount = SumTotals(livePendingOrders) + SumTotals(deletedPendingOrders);
        var cancelledAmount = SumTotals(liveCancelledOrders) + SumTotals(deletedCancelledOrders);
        var discountAmount = liveOrders.Sum(item => item.DiscountAmount);
        var surchargeAmount = liveOrders.Sum(item => item.SurchargeAmount);
        var deliveryFreightAmount = liveOrders.Sum(item => item.DeliveryFreightAmount);
        var averageTicket = paidOrdersCount > 0
            ? decimal.Round(totalSalesAmount / paidOrdersCount, 2)
            : 0;
        var generatedAtUtc = DateTime.UtcNow;
        var detailExpiresAtUtc = generatedAtUtc.AddDays(30);

        var snapshot = await _context.DailySalesSnapshots
            .FirstOrDefaultAsync(
                item => item.CompanyId == session.CompanyId &&
                        item.ReferenceDate == referenceDate,
                cancellationToken);

        if (snapshot is null)
        {
            snapshot = new DailySalesSnapshot(session.TenantId, session.CompanyId, referenceDate);
            await _context.DailySalesSnapshots.AddAsync(snapshot, cancellationToken);
        }

        snapshot.Refresh(
            ordersSubmittedCount,
            paidOrdersCount,
            pendingOrdersCount,
            cancelledOrdersCount,
            totalSalesAmount,
            paidAmount,
            pendingAmount,
            cancelledAmount,
            discountAmount,
            surchargeAmount,
            deliveryFreightAmount,
            averageTicket,
            hasDetailedData: true,
            detailExpiresAtUtc,
            generatedAtUtc);

        await _context.SaveChangesAsync(cancellationToken);

        return new DailySalesReportDto
        {
            ReferenceDate = referenceDate,
            OrdersSubmittedCount = snapshot.OrdersSubmittedCount,
            PaidOrdersCount = snapshot.PaidOrdersCount,
            PendingOrdersCount = snapshot.PendingOrdersCount,
            CancelledOrdersCount = snapshot.CancelledOrdersCount,
            TotalSalesAmount = snapshot.TotalSalesAmount,
            PaidAmount = snapshot.PaidAmount,
            PendingAmount = snapshot.PendingAmount,
            CancelledAmount = snapshot.CancelledAmount,
            DiscountAmount = snapshot.DiscountAmount,
            SurchargeAmount = snapshot.SurchargeAmount,
            DeliveryFreightAmount = snapshot.DeliveryFreightAmount,
            AverageTicket = snapshot.AverageTicket,
            PaymentMethods = BuildPaymentMethodSummaries(livePaidOrders, deletedPaidOrders),
            HasDetailedData = snapshot.HasDetailedData,
            DetailExpiresAtUtc = snapshot.DetailExpiresAtUtc
        };
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        var normalizedTimeZoneId = string.IsNullOrWhiteSpace(timeZoneId)
            ? DefaultTimeZoneId
            : timeZoneId.Trim();

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(normalizedTimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId);
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId);
        }
    }

    private static decimal SumTotals(IEnumerable<CustomerOrder> orders)
    {
        return orders.Sum(item => item.TotalAmount);
    }

    private static decimal SumTotals(IEnumerable<DeletedOrderRecord> orders)
    {
        return orders.Sum(item => item.TotalAmount);
    }

    private static IReadOnlyList<DailySalesPaymentMethodDto> BuildPaymentMethodSummaries(
        IReadOnlyList<CustomerOrder> livePaidOrders,
        IReadOnlyList<DeletedOrderRecord> deletedPaidOrders)
    {
        var entries = new List<PaymentMethodReportEntry>();

        foreach (var order in livePaidOrders)
        {
            var activePayments = order.Payments
                .Where(item => item.IsActive)
                .ToList();

            if (activePayments.Count > 0)
            {
                entries.AddRange(activePayments.Select(payment => new PaymentMethodReportEntry(
                    payment.Method,
                    payment.Amount,
                    order.Id)));
                continue;
            }

            entries.Add(new PaymentMethodReportEntry(order.PaymentMethod, order.TotalAmount, order.Id));
        }

        entries.AddRange(deletedPaidOrders.Select(order => new PaymentMethodReportEntry(
            order.PaymentMethod,
            order.TotalAmount,
            order.SourceOrderId)));

        return entries
            .GroupBy(item => item.Method)
            .OrderBy(item => item.Key)
            .Select(group => new DailySalesPaymentMethodDto
            {
                Method = group.Key.ToString(),
                Amount = decimal.Round(group.Sum(item => item.Amount), 2),
                OrdersCount = group.Select(item => item.OrderId).Distinct().Count()
            })
            .ToList();
    }

    private readonly record struct PaymentMethodReportEntry(
        PaymentMethod Method,
        decimal Amount,
        Guid OrderId);
}
