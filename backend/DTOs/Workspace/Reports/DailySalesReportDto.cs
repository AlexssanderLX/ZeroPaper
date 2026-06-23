namespace ZeroPaper.DTOs.Workspace.Reports;

public sealed class DailySalesReportDto
{
    public DateOnly ReferenceDate { get; set; }
    public int OrdersSubmittedCount { get; set; }
    public int PaidOrdersCount { get; set; }
    public int PendingOrdersCount { get; set; }
    public int CancelledOrdersCount { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal CancelledAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SurchargeAmount { get; set; }
    public decimal DeliveryFreightAmount { get; set; }
    public decimal AverageTicket { get; set; }
    public IReadOnlyList<DailySalesPaymentMethodDto> PaymentMethods { get; set; } = [];
    public bool HasDetailedData { get; set; }
    public DateTime DetailExpiresAtUtc { get; set; }
}

public sealed class DailySalesPaymentMethodDto
{
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int OrdersCount { get; set; }
}
