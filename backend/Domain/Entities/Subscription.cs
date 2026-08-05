using ZeroPaper.Domain.Common;
using ZeroPaper.Domain.Enums;
using ZeroPaper.Domain.Plans;

namespace ZeroPaper.Domain.Entities;

public class Subscription : TenantOwnedEntity
{
    public const decimal MenuModulePrice = 9.90m;
    public const decimal TablesModulePrice = 10.00m;
    public const decimal KitchenModulePrice = 12.00m;
    public const decimal CashModulePrice = 12.00m;
    public const decimal StockModulePrice = 9.00m;
    public const decimal DeliveryModulePrice = 10.00m;
    public const decimal PrintingModulePrice = 8.00m;
    public const decimal WaiterCallModulePrice = 9.00m;
    public const decimal AiAssistantModulePrice = 40.00m;

    private Subscription()
    {
    }

    public Subscription(
        Guid tenantId,
        string planName,
        decimal monthlyPrice,
        int maxUsers,
        DateTime startsAtUtc,
        SubscriptionStatus status = SubscriptionStatus.Trial) : base(tenantId)
    {
        ChangePlan(planName, monthlyPrice, maxUsers);
        StartsAtUtc = startsAtUtc;
        Status = status;
    }

    public string PlanName { get; private set; } = null!;
    public decimal MonthlyPrice { get; private set; }
    public int MaxUsers { get; private set; }
    public bool IncludesMenuModule { get; private set; } = true;
    public bool IncludesTablesModule { get; private set; } = true;
    public bool IncludesKitchenModule { get; private set; } = true;
    public bool IncludesCashModule { get; private set; } = true;
    public bool IncludesStockModule { get; private set; } = true;
    public bool IncludesDeliveryModule { get; private set; } = true;
    public bool IncludesPrintingModule { get; private set; } = true;
    public bool IncludesWaiterCallModule { get; private set; } = true;
    public bool IncludesAiAssistantModule { get; private set; }
    public DateTime StartsAtUtc { get; private set; }
    public DateTime? EndsAtUtc { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public string? MercadoPagoPreapprovalId { get; private set; }
    public string? MercadoPagoPreapprovalPlanId { get; private set; }
    public string? MercadoPagoCheckoutUrl { get; private set; }
    public string? MercadoPagoStatus { get; private set; }
    public DateTime? MercadoPagoStatusUpdatedAtUtc { get; private set; }
    public DateTime? PaidThroughUtc { get; private set; }
    public string? CheckoutConfirmationTokenHash { get; private set; }

    public Tenant Tenant { get; private set; } = null!;

    public void ChangePlan(string planName, decimal monthlyPrice, int maxUsers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(planName);

        if (monthlyPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monthlyPrice));
        }

        if (maxUsers <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxUsers));
        }

        PlanName = planName.Trim();
        MonthlyPrice = decimal.Round(monthlyPrice, 2);
        MaxUsers = maxUsers;
        Touch();
    }

    public void UpdateFeatureSet(
        bool includesMenuModule,
        bool includesTablesModule,
        bool includesKitchenModule,
        bool includesCashModule,
        bool includesStockModule,
        bool includesDeliveryModule,
        bool includesPrintingModule,
        bool includesWaiterCallModule,
        bool includesAiAssistantModule)
    {
        IncludesMenuModule = includesMenuModule;
        IncludesTablesModule = includesTablesModule;
        IncludesKitchenModule = includesKitchenModule;
        IncludesCashModule = includesCashModule;
        IncludesStockModule = includesStockModule;
        IncludesDeliveryModule = includesDeliveryModule;
        IncludesPrintingModule = includesPrintingModule;
        IncludesWaiterCallModule = includesWaiterCallModule;
        IncludesAiAssistantModule = includesAiAssistantModule;
        Touch();
    }

    public void ApplyCommercialPlan(CommercialPlanDefinition plan, int? maxUsers = null)
    {
        ArgumentNullException.ThrowIfNull(plan);

        UpdateFeatureSet(
            plan.IncludesMenuModule,
            plan.IncludesTablesModule,
            plan.IncludesKitchenModule,
            plan.IncludesCashModule,
            plan.IncludesStockModule,
            plan.IncludesDeliveryModule,
            plan.IncludesPrintingModule,
            plan.IncludesWaiterCallModule,
            plan.IncludesAiAssistantModule);

        ChangePlan(plan.Name, plan.MonthlyPrice, maxUsers.GetValueOrDefault(plan.DefaultMaxUsers));
    }

    public void Reactivate()
    {
        Status = SubscriptionStatus.Active;
        EndsAtUtc = null;
        Touch();
    }

    public void Suspend()
    {
        Status = SubscriptionStatus.Suspended;
        Touch();
    }

    public void Cancel(DateTime endsAtUtc)
    {
        if (endsAtUtc < StartsAtUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(endsAtUtc));
        }

        Status = SubscriptionStatus.Cancelled;
        EndsAtUtc = endsAtUtc;
        Touch();
    }

    public void RegisterMercadoPagoCheckout(string preapprovalId, string checkoutUrl, string status, DateTime occurredAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(preapprovalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(checkoutUrl);
        MercadoPagoPreapprovalId = preapprovalId.Trim();
        MercadoPagoCheckoutUrl = checkoutUrl.Trim();
        UpdateMercadoPagoStatus(status, occurredAtUtc);
    }

    public void RegisterMercadoPagoPlanCheckout(string preapprovalPlanId, string checkoutUrl, string status, DateTime occurredAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(preapprovalPlanId);
        ArgumentException.ThrowIfNullOrWhiteSpace(checkoutUrl);
        MercadoPagoPreapprovalPlanId = preapprovalPlanId.Trim();
        MercadoPagoCheckoutUrl = checkoutUrl.Trim();
        UpdateMercadoPagoStatus(status, occurredAtUtc);
    }

    public void UpdateMercadoPagoStatus(string status, DateTime occurredAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        MercadoPagoStatus = status.Trim().ToLowerInvariant();
        MercadoPagoStatusUpdatedAtUtc = occurredAtUtc;
        Touch();
    }

    public void SetCheckoutConfirmationTokenHash(string tokenHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        CheckoutConfirmationTokenHash = tokenHash.Trim();
        Touch();
    }

    public DateTime RegisterPaidMonth(DateTime paidAtUtc)
    {
        var basis = PaidThroughUtc.HasValue && PaidThroughUtc.Value > paidAtUtc ? PaidThroughUtc.Value : paidAtUtc;
        PaidThroughUtc = basis.AddMonths(1);
        Reactivate();
        return PaidThroughUtc.Value;
    }

    public bool HasPaidAccess(DateTime utcNow) => PaidThroughUtc.HasValue && PaidThroughUtc.Value > utcNow;
}
