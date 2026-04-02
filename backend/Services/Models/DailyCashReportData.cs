namespace ZeroPaper.Services.Models;

public class DailyCashReportData
{
    public string RestaurantName { get; init; } = string.Empty;
    public string ReportDateLabel { get; init; } = string.Empty;
    public string GeneratedAtLabel { get; init; } = string.Empty;
    public List<DailyCashReportMetric> Metrics { get; init; } = [];
    public List<DailyCashReportPaymentSummary> PaymentSummaries { get; init; } = [];
    public List<DailyCashReportOrderRow> PendingOrders { get; init; } = [];
    public List<DailyCashReportOrderRow> PaidOrders { get; init; } = [];
    public List<DailyCashReportPaymentDifferenceRow> PaymentDifferences { get; init; } = [];
    public List<DailyCashReportDeletedOrderRow> DeletedOrders { get; init; } = [];
}

public class DailyCashReportMetric
{
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string? Detail { get; init; }
}

public class DailyCashReportPaymentSummary
{
    public string Label { get; init; } = string.Empty;
    public string CountLabel { get; init; } = string.Empty;
    public string TotalLabel { get; init; } = string.Empty;
}

public class DailyCashReportOrderRow
{
    public string OrderLabel { get; init; } = string.Empty;
    public string TableLabel { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public string PaymentLabel { get; init; } = string.Empty;
    public string TotalLabel { get; init; } = string.Empty;
    public string TimeLabel { get; init; } = string.Empty;
    public string ItemsLabel { get; init; } = string.Empty;
    public string? NotesLabel { get; init; }
}

public class DailyCashReportPaymentDifferenceRow
{
    public string OrderLabel { get; init; } = string.Empty;
    public string TableLabel { get; init; } = string.Empty;
    public string RequestedPaymentLabel { get; init; } = string.Empty;
    public string AppliedPaymentLabel { get; init; } = string.Empty;
    public string TotalLabel { get; init; } = string.Empty;
    public string ContextLabel { get; init; } = string.Empty;
}

public class DailyCashReportDeletedOrderRow
{
    public string OrderLabel { get; init; } = string.Empty;
    public string TableLabel { get; init; } = string.Empty;
    public string PaymentLabel { get; init; } = string.Empty;
    public string TotalLabel { get; init; } = string.Empty;
    public string DeletedAtLabel { get; init; } = string.Empty;
    public string ReasonLabel { get; init; } = string.Empty;
    public string ItemsLabel { get; init; } = string.Empty;
}
