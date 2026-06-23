using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities
{
    public class DailySalesSnapshot : TenantOwnedEntity
    {
        private DailySalesSnapshot() { }


        public DailySalesSnapshot(
            Guid tenantId,
            Guid companyId,
            DateOnly referenceDate) : base(tenantId)
        {
            CompanyId = companyId;
            ReferenceDate = referenceDate;
            Refresh(
                ordersSubmittedCount: 0,
                paidOrdersCount: 0,
                pendingOrdersCount: 0,
                cancelledOrdersCount: 0,
                totalSalesAmount: 0,
                paidAmount: 0,
                pendingAmount: 0,
                cancelledAmount: 0,
                discountAmount: 0,
                surchargeAmount: 0,
                deliveryFreightAmount: 0,
                averageTicket: 0,
                hasDetailedData: true,
                detailExpiresAtUtc: DateTime.UtcNow.AddDays(30),
                generatedAtUtc: DateTime.UtcNow);
        }
        public Guid CompanyId { get; private set; }
        public DateOnly ReferenceDate { get; private set; }

        public int OrdersSubmittedCount { get; private set; }
        public int PaidOrdersCount { get; private set; }
        public int PendingOrdersCount { get; private set; }
        public int CancelledOrdersCount { get; private set; }

        public decimal TotalSalesAmount { get; private set; }
        public decimal PaidAmount { get; private set; }
        public decimal PendingAmount { get; private set; }
        public decimal CancelledAmount { get; private set; }

        public decimal DiscountAmount { get; private set; }
        public decimal SurchargeAmount { get; private set; }
        public decimal DeliveryFreightAmount { get; private set; }
        public decimal AverageTicket { get; private set; }

        public bool HasDetailedData { get; private set; }
        public DateTime DetailExpiresAtUtc { get; private set; }
        public DateTime? DetailPurgedAtUtc { get; private set; }
        public DateTime GeneratedAtUtc { get; private set; }

        public Company Company { get; private set; } = null!;

        public void Refresh(
            int ordersSubmittedCount,
            int paidOrdersCount,
            int pendingOrdersCount,
            int cancelledOrdersCount,
            decimal totalSalesAmount,
            decimal paidAmount,
            decimal pendingAmount,
            decimal cancelledAmount,
            decimal discountAmount,
            decimal surchargeAmount,
            decimal deliveryFreightAmount,
            decimal averageTicket,
            bool hasDetailedData,
            DateTime detailExpiresAtUtc,
            DateTime generatedAtUtc)
        {
            OrdersSubmittedCount = ordersSubmittedCount;
            PaidOrdersCount = paidOrdersCount;
            PendingOrdersCount = pendingOrdersCount;
            CancelledOrdersCount = cancelledOrdersCount;
            TotalSalesAmount = decimal.Round(totalSalesAmount, 2);
            PaidAmount = decimal.Round(paidAmount, 2);
            PendingAmount = decimal.Round(pendingAmount, 2);
            CancelledAmount = decimal.Round(cancelledAmount, 2);
            DiscountAmount = decimal.Round(discountAmount, 2);
            SurchargeAmount = decimal.Round(surchargeAmount, 2);
            DeliveryFreightAmount = decimal.Round(deliveryFreightAmount, 2);
            AverageTicket = decimal.Round(averageTicket, 2);
            HasDetailedData = hasDetailedData;
            DetailExpiresAtUtc = detailExpiresAtUtc;
            DetailPurgedAtUtc = hasDetailedData ? null : DetailPurgedAtUtc;
            GeneratedAtUtc = generatedAtUtc;
            Touch();
        }
    }
}
