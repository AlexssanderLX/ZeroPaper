using ZeroPaper.Domain.Common;
using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Domain.Entities;

public class CustomerOrder : TenantOwnedEntity
{
    private const int AutomaticPrintRetryLimit = 2;
    private readonly List<OrderItem> _items = [];
    private readonly List<CustomerOrderPayment> _payments = [];

    private CustomerOrder()
    {
    }

    public CustomerOrder(
        Guid tenantId,
        Guid companyId,
        Guid diningTableId,
        int number,
        string? customerName,
        string? notes,
        string? deliveryPhone,
        string? deliveryAddress,
        string? deliveryNumber,
        string? deliveryComplement,
        string? deliveryPostalCode,
        decimal deliveryFreightAmount,
        decimal? deliveryDistanceKm,
        string? deliveryFreightProvider,
        IEnumerable<OrderItem> items,
        PaymentMethod paymentMethod) : base(tenantId)
    {
        CompanyId = companyId;
        DiningTableId = diningTableId;
        Number = number;
        CustomerName = string.IsNullOrWhiteSpace(customerName) ? null : customerName.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        DeliveryPhone = string.IsNullOrWhiteSpace(deliveryPhone) ? null : deliveryPhone.Trim();
        DeliveryAddress = string.IsNullOrWhiteSpace(deliveryAddress) ? null : deliveryAddress.Trim();
        DeliveryNumber = string.IsNullOrWhiteSpace(deliveryNumber) ? null : deliveryNumber.Trim();
        DeliveryComplement = string.IsNullOrWhiteSpace(deliveryComplement) ? null : deliveryComplement.Trim();
        DeliveryPostalCode = NormalizePostalCodeOrNull(deliveryPostalCode);
        SetDeliveryFreight(deliveryFreightAmount, deliveryDistanceKm, deliveryFreightProvider, deliveryFreightAmount > 0 || deliveryDistanceKm.HasValue);
        SubmittedAtUtc = DateTime.UtcNow;
        Status = OrderStatus.Pending;
        PaymentMethod = paymentMethod;
        RequestedPaymentMethod = paymentMethod;
        PaymentStatus = PaymentStatus.Pending;
        PrintStatus = PrintStatus.Pending;
        PrintQueuedAtUtc = SubmittedAtUtc;

        foreach (var item in items)
        {
            _items.Add(item);
        }

        RecalculateTotal();
    }

    public Guid CompanyId { get; private set; }
    public Guid DiningTableId { get; private set; }
    public int Number { get; private set; }
    public string? CustomerName { get; private set; }
    public string? Notes { get; private set; }
    public string? DeliveryPhone { get; private set; }
    public string? DeliveryAddress { get; private set; }
    public string? DeliveryNumber { get; private set; }
    public string? DeliveryComplement { get; private set; }
    public string? DeliveryPostalCode { get; private set; }
    public decimal DeliveryFreightAmount { get; private set; }
    public decimal? DeliveryDistanceKm { get; private set; }
    public string? DeliveryFreightProvider { get; private set; }
    public DateTime? DeliveryFreightCalculatedAtUtc { get; private set; }
    public string? PublicEditCode { get; private set; }
    public DateTime? PublicEditAllowedUntilUtc { get; private set; }
    public OrderStatus Status { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public PaymentMethod RequestedPaymentMethod { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public PrintStatus PrintStatus { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal OriginalTotalAmount => decimal.Round(_items.Sum(item => item.TotalPrice) + DeliveryFreightAmount, 2);
    public bool IsEdited { get; private set; }
    public DateTime? EditedAtUtc { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal SurchargeAmount { get; private set; }
    public string? PriceAdjustmentNote { get; private set; }
    public DateTime? PriceAdjustedAtUtc { get; private set; }
    public Guid? CouponId { get; private set; }
    public string? CouponCode { get; private set; }
    public decimal CouponDiscountAmount { get; private set; }
    public DateTime? CouponAppliedAtUtc { get; private set; }
    public DateTime SubmittedAtUtc { get; private set; }
    public DateTime? SentToKitchenAtUtc { get; private set; }
    public DateTime? ReadyAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public DateTime? PrintQueuedAtUtc { get; private set; }
    public DateTime? PrintClaimedAtUtc { get; private set; }
    public DateTime? PrintedAtUtc { get; private set; }
    public int PrintAttempts { get; private set; }
    public string? PrintLastError { get; private set; }
    public string? PrintAgentName { get; private set; }
    public string? PrintPrinterName { get; private set; }

    public Tenant Tenant { get; private set; } = null!;
    public Company Company { get; private set; } = null!;
    public DiningTable DiningTable { get; private set; } = null!;
    public Coupon? Coupon { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<CustomerOrderPayment> Payments => _payments.AsReadOnly();

    public void MoveToKitchen()
    {
        if (Status == OrderStatus.Cancelled || Status == OrderStatus.Delivered)
        {
            throw new InvalidOperationException("Closed orders cannot move to kitchen.");
        }

        Status = OrderStatus.InKitchen;
        SentToKitchenAtUtc ??= DateTime.UtcNow;
        Touch();
    }

    public void MarkReady()
    {
        if (Status == OrderStatus.Cancelled || Status == OrderStatus.Delivered)
        {
            throw new InvalidOperationException("Closed orders cannot be updated.");
        }

        Status = OrderStatus.Ready;
        ReadyAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void MarkDelivered()
    {
        if (Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled orders cannot be delivered.");
        }

        Status = OrderStatus.Delivered;
        ClosedAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Delivered)
        {
            throw new InvalidOperationException("Delivered orders cannot be cancelled.");
        }

        Status = OrderStatus.Cancelled;
        ClosedAtUtc = DateTime.UtcNow;

        if (PrintStatus is PrintStatus.Pending or PrintStatus.Processing or PrintStatus.Failed)
        {
            PrintStatus = PrintStatus.Disabled;
            PrintLastError = "Pedido cancelado antes da impressao.";
        }

        Touch();
    }

    public void MarkPaid()
    {
        if (Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled orders cannot be marked as paid.");
        }

        PaymentStatus = PaymentStatus.Paid;
        PaidAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void MarkPaymentPending()
    {
        if (Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled orders cannot change payment.");
        }

        PaymentStatus = PaymentStatus.Pending;
        PaidAtUtc = null;
        _payments.Clear();
        Touch();
    }

    public void UpdatePaymentMethod(PaymentMethod paymentMethod)
    {
        if (Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled orders cannot change payment.");
        }

        PaymentMethod = paymentMethod;
        Touch();
    }

    public void UpdateOrderDetails(
        string? customerName,
        string? notes,
        string? deliveryPhone,
        string? deliveryAddress,
        string? deliveryNumber,
        string? deliveryComplement,
        string? deliveryPostalCode,
        decimal deliveryFreightAmount,
        decimal? deliveryDistanceKm,
        string? deliveryFreightProvider,
        PaymentMethod requestedPaymentMethod)
    {
        if (Status == OrderStatus.Cancelled || Status == OrderStatus.Delivered)
        {
            throw new InvalidOperationException("Closed orders cannot be edited.");
        }

        CustomerName = string.IsNullOrWhiteSpace(customerName) ? null : customerName.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        DeliveryPhone = string.IsNullOrWhiteSpace(deliveryPhone) ? null : deliveryPhone.Trim();
        DeliveryAddress = string.IsNullOrWhiteSpace(deliveryAddress) ? null : deliveryAddress.Trim();
        DeliveryNumber = string.IsNullOrWhiteSpace(deliveryNumber) ? null : deliveryNumber.Trim();
        DeliveryComplement = string.IsNullOrWhiteSpace(deliveryComplement) ? null : deliveryComplement.Trim();
        DeliveryPostalCode = NormalizePostalCodeOrNull(deliveryPostalCode);
        SetDeliveryFreight(deliveryFreightAmount, deliveryDistanceKm, deliveryFreightProvider, deliveryFreightAmount > 0 || deliveryDistanceKm.HasValue);
        RequestedPaymentMethod = requestedPaymentMethod;

        if (PaymentStatus != PaymentStatus.Paid)
        {
            PaymentMethod = requestedPaymentMethod;
        }

        Touch();
    }

    public void DisablePrinting()
    {
        PrintStatus = PrintStatus.Disabled;
        PrintQueuedAtUtc = null;
        PrintClaimedAtUtc = null;
        PrintedAtUtc = null;
        PrintLastError = null;
        Touch();
    }

    public void RequeuePrinting()
    {
        PrintStatus = PrintStatus.Pending;
        PrintQueuedAtUtc = DateTime.UtcNow;
        PrintClaimedAtUtc = null;
        PrintedAtUtc = null;
        PrintLastError = null;
        Touch();
    }

    public void ClaimPrinting(string agentName, string? printerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);

        PrintStatus = PrintStatus.Processing;
        PrintClaimedAtUtc = DateTime.UtcNow;
        PrintAttempts += 1;
        PrintLastError = null;
        PrintAgentName = agentName.Trim();
        PrintPrinterName = string.IsNullOrWhiteSpace(printerName) ? null : printerName.Trim();
        Touch();
    }

    public void MarkPrinted(string agentName, string? printerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);

        PrintStatus = PrintStatus.Printed;
        PrintedAtUtc = DateTime.UtcNow;
        PrintLastError = null;
        PrintAgentName = agentName.Trim();
        PrintPrinterName = string.IsNullOrWhiteSpace(printerName) ? null : printerName.Trim();
        Touch();
    }

    public void MarkPrintFailed(string? errorMessage, string agentName, string? printerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);

        PrintAgentName = agentName.Trim();
        PrintPrinterName = string.IsNullOrWhiteSpace(printerName) ? null : printerName.Trim();

        if (PrintAttempts < AutomaticPrintRetryLimit)
        {
            PrintStatus = PrintStatus.Pending;
            PrintQueuedAtUtc = DateTime.UtcNow;
            PrintClaimedAtUtc = null;
            PrintedAtUtc = null;
            PrintLastError = null;
            Touch();
            return;
        }

        PrintStatus = PrintStatus.Failed;
        PrintLastError = string.IsNullOrWhiteSpace(errorMessage) ? "Falha desconhecida ao imprimir." : errorMessage.Trim();
        Touch();
    }

    public void ReplaceItems(IEnumerable<OrderItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        _items.Clear();
        foreach (var item in items)
        {
            _items.Add(item);
        }

        RecalculateTotal();
    }

    public void ApplyEditedItemsTotal(decimal itemsSubtotal)
    {
        if (itemsSubtotal < 0)
        {
            throw new ArgumentException("O subtotal dos itens nao pode ser negativo.", nameof(itemsSubtotal));
        }

        var finalAmount = decimal.Round(itemsSubtotal + DeliveryFreightAmount + SurchargeAmount - DiscountAmount, 2);

        if (finalAmount < 0)
        {
            throw new InvalidOperationException("O total final do pedido nao pode ficar negativo.");
        }

        TotalAmount = finalAmount;
        Touch();
    }

    public void MarkEdited(DateTime editedAtUtc)
    {
        IsEdited = true;
        EditedAtUtc = editedAtUtc;
        Touch();
    }

    public void UpdateOwnerNotes(string? customerName, string? notes)
    {
        CustomerName = string.IsNullOrWhiteSpace(customerName) ? null : customerName.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Touch();
    }

    public void ApplyPriceAdjustment(decimal discountAmount, decimal surchargeAmount, string? note, DateTime adjustedAtUtc)
    {
        if (discountAmount < 0)
        {
            throw new ArgumentException("O desconto nao pode ser negativo.", nameof(discountAmount));
        }

        if (surchargeAmount < 0)
        {
            throw new ArgumentException("O acrescimo nao pode ser negativo.", nameof(surchargeAmount));
        }

        ClearCoupon();
        DiscountAmount = decimal.Round(discountAmount, 2);
        SurchargeAmount = decimal.Round(surchargeAmount, 2);
        PriceAdjustmentNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        PriceAdjustedAtUtc = DiscountAmount > 0 || SurchargeAmount > 0 || PriceAdjustmentNote is not null
            ? adjustedAtUtc
            : null;

        RecalculateTotal();
    }

    public void ApplyCoupon(Coupon coupon, decimal discountAmount, DateTime appliedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(coupon);

        if (discountAmount < 0)
        {
            throw new ArgumentException("O desconto do cupom nao pode ser negativo.", nameof(discountAmount));
        }

        var roundedDiscount = decimal.Round(discountAmount, 2);
        if (roundedDiscount > OriginalTotalAmount)
        {
            throw new InvalidOperationException("O desconto do cupom nao pode superar o subtotal do pedido.");
        }

        CouponId = coupon.Id;
        CouponCode = coupon.Code;
        CouponDiscountAmount = roundedDiscount;
        CouponAppliedAtUtc = appliedAtUtc;
        DiscountAmount = roundedDiscount;
        PriceAdjustmentNote = roundedDiscount > 0 ? $"Cupom {coupon.Code}" : null;
        PriceAdjustedAtUtc = roundedDiscount > 0 ? appliedAtUtc : null;
        RecalculateTotal();
    }

    public void ClearCoupon()
    {
        CouponId = null;
        CouponCode = null;
        CouponDiscountAmount = 0m;
        CouponAppliedAtUtc = null;
    }

    public void ApplyFinalAmountAdjustment(decimal finalAmount, string? note, DateTime adjustedAtUtc)
    {
        if (finalAmount < 0)
        {
            throw new ArgumentException("O valor final nao pode ser negativo.", nameof(finalAmount));
        }

        var roundedFinalAmount = decimal.Round(finalAmount, 2);
        var originalAmount = OriginalTotalAmount;
        var discountAmount = roundedFinalAmount < originalAmount ? originalAmount - roundedFinalAmount : 0m;
        var surchargeAmount = roundedFinalAmount > originalAmount ? roundedFinalAmount - originalAmount : 0m;

        ApplyPriceAdjustment(discountAmount, surchargeAmount, note, adjustedAtUtc);
    }

    public void ReplacePayments(IEnumerable<CustomerOrderPayment> payments)
    {
        ArgumentNullException.ThrowIfNull(payments);

        var materializedPayments = payments.ToList();
        var paymentTotal = decimal.Round(materializedPayments.Sum(item => item.Amount), 2);

        if (paymentTotal != TotalAmount)
        {
            throw new InvalidOperationException("A soma dos pagamentos precisa bater com o total final do pedido.");
        }

        _payments.Clear();
        _payments.AddRange(materializedPayments);
        PaymentStatus = PaymentStatus.Paid;
        PaidAtUtc = DateTime.UtcNow;
        PaymentMethod = materializedPayments.Count == 1 ? materializedPayments[0].Method : PaymentMethod.Undefined;
        Touch();
    }

    private void RecalculateTotal()
    {
        var finalAmount = OriginalTotalAmount + SurchargeAmount - DiscountAmount;

        if (finalAmount < 0)
        {
            throw new InvalidOperationException("O total final do pedido nao pode ficar negativo.");
        }

        TotalAmount = decimal.Round(finalAmount, 2);
        Touch();
    }

    private void SetDeliveryFreight(decimal amount, decimal? distanceKm, string? provider, bool markAsCalculated)
    {
        if (amount < 0)
        {
            throw new ArgumentException("O frete nao pode ser negativo.", nameof(amount));
        }

        if (distanceKm.HasValue && distanceKm.Value < 0)
        {
            throw new ArgumentException("A distancia do frete nao pode ser negativa.", nameof(distanceKm));
        }

        DeliveryFreightAmount = decimal.Round(amount, 2);
        DeliveryDistanceKm = distanceKm.HasValue ? decimal.Round(distanceKm.Value, 2) : null;
        DeliveryFreightProvider = string.IsNullOrWhiteSpace(provider) ? null : provider.Trim();
        DeliveryFreightCalculatedAtUtc = markAsCalculated ? DateTime.UtcNow : null;
    }

    private static string? NormalizePostalCodeOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 8 ? digits : value.Trim();
    }

    public void ReturnToPreparation()
    {
        if (Status != OrderStatus.Ready)
        {
            throw new InvalidOperationException("Apenas pedidos prontos podem voltar para preparo.");
        }

        Status = OrderStatus.InKitchen;
        ReadyAtUtc = null;
        SentToKitchenAtUtc ??= DateTime.UtcNow;
        Touch();
    }

}
