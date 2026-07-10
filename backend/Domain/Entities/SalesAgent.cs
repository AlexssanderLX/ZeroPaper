using System.Security.Cryptography;
using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;


public class SalesAgent : TenantOwnedEntity
{
    private SalesAgent()
    {
    }

    public SalesAgent(Guid tenantId, Guid companyId, string name, string? phone, decimal? commissionPercent)
        : base(tenantId)
    {
        CompanyId = companyId;
        Name = name.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        CommissionPercent = commissionPercent;
        Code = GenerateCode();
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public decimal? CommissionPercent { get; private set; }

    public void UpdateInfo(string name, string? phone, decimal? commissionPercent)
    {
        Name = name.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        CommissionPercent = commissionPercent;
        Touch();
    }

    public Company Company { get; private set; } = null!;

    private static string GenerateCode()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(6)).ToLowerInvariant();
}
