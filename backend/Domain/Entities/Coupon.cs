using ZeroPaper.Domain.Common;
using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Domain.Entities;

public class Coupon : TenantOwnedEntity
{
    private Coupon()
    {
    }

    public Coupon(
        Guid tenantId,
        Guid companyId,
        string code,
        string? description,
        CouponDiscountType discountType,
        decimal discountValue,
        decimal minimumOrderAmount,
        DateTime? startsAtUtc,
        DateTime? endsAtUtc,
        int? usageLimit,
        bool isActive = true) : base(tenantId)
    {
        CompanyId = companyId;
        Update(code, description, discountType, discountValue, minimumOrderAmount, startsAtUtc, endsAtUtc, usageLimit);
        IsActive = isActive;
    }

    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public CouponDiscountType DiscountType { get; private set; }
    public decimal DiscountValue { get; private set; }
    public decimal MinimumOrderAmount { get; private set; }
    public DateTime? StartsAtUtc { get; private set; }
    public DateTime? EndsAtUtc { get; private set; }
    public int? UsageLimit { get; private set; }
    public int UsageCount { get; private set; }

    public Tenant Tenant { get; private set; } = null!;
    public Company Company { get; private set; } = null!;

    public void Update(
        string code,
        string? description,
        CouponDiscountType discountType,
        decimal discountValue,
        decimal minimumOrderAmount,
        DateTime? startsAtUtc,
        DateTime? endsAtUtc,
        int? usageLimit)
    {
        Code = NormalizeCode(code);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        DiscountType = discountType;
        DiscountValue = NormalizeDiscountValue(discountType, discountValue);
        MinimumOrderAmount = NormalizeMinimumOrderAmount(minimumOrderAmount);
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        UsageLimit = NormalizeUsageLimit(usageLimit);

        if (StartsAtUtc.HasValue && EndsAtUtc.HasValue && EndsAtUtc <= StartsAtUtc)
        {
            throw new ArgumentException("A data final do cupom precisa ser posterior ao inicio.");
        }

        Touch();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        Touch();
    }

    public void IncrementUsage()
    {
        if (UsageLimit.HasValue && UsageCount >= UsageLimit.Value)
        {
            throw new InvalidOperationException("O limite de uso deste cupom foi atingido.");
        }

        UsageCount += 1;
        Touch();
    }

    public decimal CalculateDiscount(decimal eligibleAmount)
    {
        if (eligibleAmount <= 0)
        {
            return 0m;
        }

        var discount = DiscountType == CouponDiscountType.Percent
            ? eligibleAmount * (DiscountValue / 100m)
            : DiscountValue;

        return decimal.Round(Math.Min(discount, eligibleAmount), 2);
    }

    public static string NormalizeCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length > 40)
        {
            throw new ArgumentException("O codigo do cupom deve ter no maximo 40 caracteres.", nameof(code));
        }

        return normalized;
    }

    private static decimal NormalizeDiscountValue(CouponDiscountType discountType, decimal value)
    {
        var rounded = decimal.Round(value, 2);
        return discountType switch
        {
            CouponDiscountType.Percent when rounded is <= 0m or > 100m =>
                throw new ArgumentException("O percentual do cupom precisa ficar entre 0,01 e 100."),
            CouponDiscountType.FixedAmount when rounded <= 0m =>
                throw new ArgumentException("O valor fixo do cupom precisa ser maior que zero."),
            CouponDiscountType.Percent or CouponDiscountType.FixedAmount => rounded,
            _ => throw new ArgumentException("Tipo de cupom invalido.")
        };
    }

    private static decimal NormalizeMinimumOrderAmount(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentException("O valor minimo do pedido nao pode ser negativo.", nameof(value));
        }

        return decimal.Round(value, 2);
    }

    private static int? NormalizeUsageLimit(int? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        if (value.Value <= 0)
        {
            throw new ArgumentException("O limite de uso precisa ser maior que zero.", nameof(value));
        }

        return value.Value;
    }
}
