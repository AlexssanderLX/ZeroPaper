using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.Domain.Plans;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class CouponService : ICouponService
{
    private readonly ZeroPaperDbContext _context;

    public CouponService(ZeroPaperDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CouponDto>> GetCouponsAsync(
        WorkspaceSessionContext session,
        CancellationToken cancellationToken = default)
    {
        EnsureCouponsEnabled(session);

        return await _context.Coupons
            .AsNoTracking()
            .Where(item => item.CompanyId == session.CompanyId)
            .OrderByDescending(item => item.IsActive)
            .ThenBy(item => item.Code)
            .Select(item => MapCoupon(item))
            .ToListAsync(cancellationToken);
    }

    public async Task<CouponDto> CreateCouponAsync(
        WorkspaceSessionContext session,
        SaveCouponRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsureCouponsEnabled(session);
        ArgumentNullException.ThrowIfNull(request);

        var discountType = ParseDiscountType(request.DiscountType);
        var normalizedCode = Coupon.NormalizeCode(request.Code);
        await EnsureCodeIsAvailableAsync(session.CompanyId, normalizedCode, excludedCouponId: null, cancellationToken);

        var coupon = new Coupon(
            session.TenantId,
            session.CompanyId,
            normalizedCode,
            request.Description,
            discountType,
            request.DiscountValue,
            request.MinimumOrderAmount,
            request.StartsAtUtc,
            request.EndsAtUtc,
            request.UsageLimit);

        await _context.Coupons.AddAsync(coupon, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return MapCoupon(coupon);
    }

    public async Task<CouponDto> UpdateCouponAsync(
        WorkspaceSessionContext session,
        Guid couponId,
        SaveCouponRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsureCouponsEnabled(session);
        ArgumentNullException.ThrowIfNull(request);

        var coupon = await GetCouponEntityAsync(session.CompanyId, couponId, cancellationToken);
        var discountType = ParseDiscountType(request.DiscountType);
        var normalizedCode = Coupon.NormalizeCode(request.Code);
        await EnsureCodeIsAvailableAsync(session.CompanyId, normalizedCode, coupon.Id, cancellationToken);

        coupon.Update(
            normalizedCode,
            request.Description,
            discountType,
            request.DiscountValue,
            request.MinimumOrderAmount,
            request.StartsAtUtc,
            request.EndsAtUtc,
            request.UsageLimit);

        await _context.SaveChangesAsync(cancellationToken);
        return MapCoupon(coupon);
    }

    public async Task<CouponDto> UpdateCouponStatusAsync(
        WorkspaceSessionContext session,
        Guid couponId,
        UpdateCouponStatusRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsureCouponsEnabled(session);
        ArgumentNullException.ThrowIfNull(request);

        var coupon = await GetCouponEntityAsync(session.CompanyId, couponId, cancellationToken);
        coupon.SetActive(request.IsActive);
        await _context.SaveChangesAsync(cancellationToken);
        return MapCoupon(coupon);
    }

    public async Task<CouponValidationDto> ValidateWorkspaceCouponAsync(
        WorkspaceSessionContext session,
        ValidateCouponRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsureCouponsEnabled(session);
        ArgumentNullException.ThrowIfNull(request);
        return await ValidateForCompanyAsync(session.CompanyId, request.Code, request.Subtotal, DateTime.UtcNow, cancellationToken);
    }

    public async Task<CouponValidationDto> ValidatePublicCouponAsync(
        string publicCode,
        ValidateCouponRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicCode);
        ArgumentNullException.ThrowIfNull(request);

        var normalizedPublicCode = publicCode.Trim().ToLowerInvariant();
        var table = await _context.DiningTables
            .AsNoTracking()
            .Include(item => item.QrCodeAccess)
            .FirstOrDefaultAsync(
                item => item.QrCodeAccess.PublicCode == normalizedPublicCode &&
                        item.IsActive &&
                        item.QrCodeAccess.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Mesa nao encontrada.");

        if (!await CompanyHasCouponsAsync(table.TenantId, cancellationToken))
        {
            return Invalid(request.Code, request.Subtotal, "Cupons nao fazem parte do plano atual da unidade.");
        }

        return await ValidateForCompanyAsync(table.CompanyId, request.Code, request.Subtotal, DateTime.UtcNow, cancellationToken);
    }

    public async Task ApplyCouponToOrderAsync(
        Guid companyId,
        CustomerOrder order,
        string? couponCode,
        decimal eligibleSubtotal,
        bool incrementUsage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            return;
        }

        var coupon = await GetValidCouponOrThrowAsync(companyId, couponCode, eligibleSubtotal, DateTime.UtcNow, cancellationToken);
        var discountAmount = coupon.CalculateDiscount(eligibleSubtotal);
        order.ApplyCoupon(coupon, discountAmount, DateTime.UtcNow);

        if (incrementUsage)
        {
            coupon.IncrementUsage();
        }
    }

    public async Task ReapplyOrderCouponAsync(
        CustomerOrder order,
        decimal eligibleSubtotal,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(order.CouponCode))
        {
            return;
        }

        var coupon = await GetValidCouponOrThrowAsync(order.CompanyId, order.CouponCode, eligibleSubtotal, DateTime.UtcNow, cancellationToken);
        order.ApplyCoupon(coupon, coupon.CalculateDiscount(eligibleSubtotal), DateTime.UtcNow);
    }

    private async Task<CouponValidationDto> ValidateForCompanyAsync(
        Guid companyId,
        string? code,
        decimal subtotal,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (subtotal < 0)
        {
            throw new ArgumentException("O subtotal nao pode ser negativo.", nameof(subtotal));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return Invalid(code, subtotal, "Informe um codigo de cupom.");
        }

        var coupon = await FindCouponByCodeAsync(companyId, code, cancellationToken);
        if (coupon is null)
        {
            return Invalid(code, subtotal, "Cupom nao encontrado.");
        }

        var invalidMessage = GetInvalidReason(coupon, subtotal, nowUtc);
        if (invalidMessage is not null)
        {
            return Invalid(coupon.Code, subtotal, invalidMessage, coupon);
        }

        var discountAmount = coupon.CalculateDiscount(subtotal);
        return new CouponValidationDto
        {
            IsValid = true,
            Code = coupon.Code,
            Message = "Cupom aplicado.",
            DiscountAmount = discountAmount,
            FinalSubtotal = decimal.Round(subtotal - discountAmount, 2),
            Coupon = MapCoupon(coupon)
        };
    }

    private async Task<Coupon> GetValidCouponOrThrowAsync(
        Guid companyId,
        string code,
        decimal eligibleSubtotal,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (eligibleSubtotal < 0)
        {
            throw new ArgumentException("O subtotal nao pode ser negativo.", nameof(eligibleSubtotal));
        }

        var coupon = await FindCouponByCodeAsync(companyId, code, cancellationToken)
            ?? throw new InvalidOperationException("Cupom nao encontrado.");

        var invalidReason = GetInvalidReason(coupon, eligibleSubtotal, nowUtc);
        if (invalidReason is not null)
        {
            throw new InvalidOperationException(invalidReason);
        }

        return coupon;
    }

    private async Task<Coupon?> FindCouponByCodeAsync(Guid companyId, string code, CancellationToken cancellationToken)
    {
        var normalizedCode = Coupon.NormalizeCode(code);
        return await _context.Coupons
            .FirstOrDefaultAsync(
                item => item.CompanyId == companyId &&
                        item.Code == normalizedCode,
                cancellationToken);
    }

    private async Task<Coupon> GetCouponEntityAsync(Guid companyId, Guid couponId, CancellationToken cancellationToken)
    {
        return await _context.Coupons
            .FirstOrDefaultAsync(
                item => item.Id == couponId &&
                        item.CompanyId == companyId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Cupom nao encontrado.");
    }

    private async Task EnsureCodeIsAvailableAsync(
        Guid companyId,
        string code,
        Guid? excludedCouponId,
        CancellationToken cancellationToken)
    {
        var exists = await _context.Coupons.AnyAsync(
            item => item.CompanyId == companyId &&
                    item.Code == code &&
                    (!excludedCouponId.HasValue || item.Id != excludedCouponId.Value),
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("Ja existe um cupom com este codigo nesta unidade.");
        }
    }

    private async Task<bool> CompanyHasCouponsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var planName = await _context.Subscriptions
            .AsNoTracking()
            .Where(item =>
                item.TenantId == tenantId &&
                item.IsActive &&
                item.Status == SubscriptionStatus.Active)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => item.PlanName)
            .FirstOrDefaultAsync(cancellationToken);

        return CommercialPlanCatalog.ResolveFeatures(planName).HasCoupons;
    }

    private static string? GetInvalidReason(Coupon coupon, decimal subtotal, DateTime nowUtc)
    {
        if (!coupon.IsActive)
        {
            return "Cupom inativo.";
        }

        if (coupon.StartsAtUtc.HasValue && nowUtc < coupon.StartsAtUtc.Value)
        {
            return "Cupom ainda nao esta disponivel.";
        }

        if (coupon.EndsAtUtc.HasValue && nowUtc > coupon.EndsAtUtc.Value)
        {
            return "Cupom expirado.";
        }

        if (coupon.UsageLimit.HasValue && coupon.UsageCount >= coupon.UsageLimit.Value)
        {
            return "O limite de uso deste cupom foi atingido.";
        }

        if (subtotal < coupon.MinimumOrderAmount)
        {
            return $"Este cupom exige pedido minimo de R$ {coupon.MinimumOrderAmount:N2}.";
        }

        return null;
    }

    private static CouponValidationDto Invalid(string? code, decimal subtotal, string message, Coupon? coupon = null)
    {
        return new CouponValidationDto
        {
            IsValid = false,
            Code = string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim().ToUpperInvariant(),
            Message = message,
            DiscountAmount = 0m,
            FinalSubtotal = decimal.Round(Math.Max(0m, subtotal), 2),
            Coupon = coupon is null ? null : MapCoupon(coupon)
        };
    }

    private static CouponDiscountType ParseDiscountType(string? value)
    {
        if (Enum.TryParse<CouponDiscountType>(value, true, out var parsed))
        {
            return parsed;
        }

        throw new ArgumentException("Tipo de cupom invalido.", nameof(value));
    }

    private static void EnsureCouponsEnabled(WorkspaceSessionContext session)
    {
        if (!session.HasCoupons)
        {
            throw new UnauthorizedAccessException("Cupons nao fazem parte do plano atual da unidade.");
        }
    }

    private static CouponDto MapCoupon(Coupon item)
    {
        return new CouponDto
        {
            Id = item.Id,
            Code = item.Code,
            Description = item.Description,
            DiscountType = item.DiscountType.ToString(),
            DiscountValue = item.DiscountValue,
            MinimumOrderAmount = item.MinimumOrderAmount,
            StartsAtUtc = item.StartsAtUtc,
            EndsAtUtc = item.EndsAtUtc,
            IsActive = item.IsActive,
            UsageLimit = item.UsageLimit,
            UsageCount = item.UsageCount,
            CreatedAtUtc = item.CreatedAtUtc,
            UpdatedAtUtc = item.UpdatedAtUtc
        };
    }
}
