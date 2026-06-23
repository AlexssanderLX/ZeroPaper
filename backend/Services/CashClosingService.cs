using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class CashClosingService : ICashClosingService
{
    private const string DefaultTimeZoneId = "America/Sao_Paulo";
    private readonly ZeroPaperDbContext _context;

    public CashClosingService(ZeroPaperDbContext context)
    {
        _context = context;
    }

    public async Task<CashClosingReportDto> GetCashClosingAsync(
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

        var paidLiveOrders = liveOrders
            .Where(item => item.Status != OrderStatus.Cancelled && item.PaymentStatus == PaymentStatus.Paid)
            .ToList();
        var paidDeletedOrders = deletedOrders
            .Where(item => item.Status != OrderStatus.Cancelled && item.PaymentStatus == PaymentStatus.Paid)
            .ToList();

        var totalSold = paidLiveOrders.Sum(item => item.TotalAmount) + paidDeletedOrders.Sum(item => item.TotalAmount);
        var ordersCount = paidLiveOrders.Count + paidDeletedOrders.Count;
        var cancelledOrdersCount = liveOrders.Count(item => item.Status == OrderStatus.Cancelled) +
                                   deletedOrders.Count(item => item.Status == OrderStatus.Cancelled);
        var discountsTotal = paidLiveOrders.Sum(item => item.DiscountAmount);
        var averageTicket = ordersCount > 0 ? decimal.Round(totalSold / ordersCount, 2) : 0m;

        return new CashClosingReportDto
        {
            ReferenceDate = referenceDate,
            TotalSold = decimal.Round(totalSold, 2),
            OrdersCount = ordersCount,
            AverageTicket = averageTicket,
            DiscountsTotal = decimal.Round(discountsTotal, 2),
            CancelledOrdersCount = cancelledOrdersCount,
            PaymentMethods = BuildPaymentMethods(paidLiveOrders, paidDeletedOrders)
        };
    }

    private static IReadOnlyList<CashClosingPaymentMethodDto> BuildPaymentMethods(
        IReadOnlyList<CustomerOrder> livePaidOrders,
        IReadOnlyList<DeletedOrderRecord> deletedPaidOrders)
    {
        var entries = new List<PaymentMethodEntry>();

        foreach (var order in livePaidOrders)
        {
            var payments = order.Payments.Where(item => item.IsActive).ToList();
            if (payments.Count > 0)
            {
                entries.AddRange(payments.Select(payment => new PaymentMethodEntry(
                    NormalizePaymentMethod(payment.Method),
                    payment.Amount,
                    order.Id)));
                continue;
            }

            entries.Add(new PaymentMethodEntry(NormalizePaymentMethod(order.PaymentMethod), order.TotalAmount, order.Id));
        }

        entries.AddRange(deletedPaidOrders.Select(order => new PaymentMethodEntry(
            NormalizePaymentMethod(order.PaymentMethod),
            order.TotalAmount,
            order.SourceOrderId)));

        var lookup = entries
            .GroupBy(item => item.Method)
            .ToDictionary(
                group => group.Key,
                group => new CashClosingPaymentMethodDto
                {
                    Method = group.Key,
                    Amount = decimal.Round(group.Sum(item => item.Amount), 2),
                    OrdersCount = group.Select(item => item.OrderId).Distinct().Count()
                });

        var orderedMethods = new[] { "Pix", "Cash", "Credit", "Debit", "Other" };
        return orderedMethods
            .Select(method => lookup.TryGetValue(method, out var value)
                ? value
                : new CashClosingPaymentMethodDto { Method = method, Amount = 0m, OrdersCount = 0 })
            .ToList();
    }

    private static string NormalizePaymentMethod(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.Pix => "Pix",
            PaymentMethod.Cash => "Cash",
            PaymentMethod.Credit => "Credit",
            PaymentMethod.Debit => "Debit",
            _ => "Other"
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

    private readonly record struct PaymentMethodEntry(string Method, decimal Amount, Guid OrderId);
}
