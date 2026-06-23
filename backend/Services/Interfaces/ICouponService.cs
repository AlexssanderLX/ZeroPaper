using ZeroPaper.Domain.Entities;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Interfaces;

public interface ICouponService
{
    Task<IReadOnlyList<CouponDto>> GetCouponsAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<CouponDto> CreateCouponAsync(WorkspaceSessionContext session, SaveCouponRequestDto request, CancellationToken cancellationToken = default);
    Task<CouponDto> UpdateCouponAsync(WorkspaceSessionContext session, Guid couponId, SaveCouponRequestDto request, CancellationToken cancellationToken = default);
    Task<CouponDto> UpdateCouponStatusAsync(WorkspaceSessionContext session, Guid couponId, UpdateCouponStatusRequestDto request, CancellationToken cancellationToken = default);
    Task<CouponValidationDto> ValidateWorkspaceCouponAsync(WorkspaceSessionContext session, ValidateCouponRequestDto request, CancellationToken cancellationToken = default);
    Task<CouponValidationDto> ValidatePublicCouponAsync(string publicCode, ValidateCouponRequestDto request, CancellationToken cancellationToken = default);
    Task ApplyCouponToOrderAsync(Guid companyId, CustomerOrder order, string? couponCode, decimal eligibleSubtotal, bool incrementUsage, CancellationToken cancellationToken = default);
    Task ReapplyOrderCouponAsync(CustomerOrder order, decimal eligibleSubtotal, CancellationToken cancellationToken = default);
}
