using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.Domain.Plans;
using ZeroPaper.DTOs.Onboarding;
using ZeroPaper.DTOs.Public;
using ZeroPaper.Repositories.Interfaces;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Services;

public class RestaurantOnboardingService : IRestaurantOnboardingService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> SignupLocks = new(StringComparer.Ordinal);

    private readonly ZeroPaperDbContext _context;
    private readonly ITenantRepository _tenantRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IAppUserRepository _appUserRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IQrCodeAccessRepository _qrCodeAccessRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessRequestNotificationService _accessRequestNotificationService;
    private readonly ICashOrderTableService _cashOrderTableService;

    public RestaurantOnboardingService(
        ZeroPaperDbContext context,
        ITenantRepository tenantRepository,
        ICompanyRepository companyRepository,
        IAppUserRepository appUserRepository,
        ISubscriptionRepository subscriptionRepository,
        IQrCodeAccessRepository qrCodeAccessRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IAccessRequestNotificationService accessRequestNotificationService,
        ICashOrderTableService cashOrderTableService)
    {
        _context = context;
        _tenantRepository = tenantRepository;
        _companyRepository = companyRepository;
        _appUserRepository = appUserRepository;
        _subscriptionRepository = subscriptionRepository;
        _qrCodeAccessRepository = qrCodeAccessRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _accessRequestNotificationService = accessRequestNotificationService;
        _cashOrderTableService = cashOrderTableService;
    }

    public async Task<RestaurantOnboardingResponseDto> CreateAsync(
        RestaurantOnboardingRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.BusinessSegment != BusinessSegment.Restaurant)
        {
            throw new InvalidOperationException("Este segmento ainda esta em desenvolvimento.");
        }

        var normalizedOwnerEmail = NormalizeEmail(request.OwnerEmail);
        var lockKey = normalizedOwnerEmail;
        var signupLock = SignupLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

        await signupLock.WaitAsync(cancellationToken);
        try
        {
            var existingSignup = await FindExistingSignupAsync(normalizedOwnerEmail, cancellationToken);
            if (existingSignup is not null)
            {
                return existingSignup;
            }

            var signupCode = string.IsNullOrWhiteSpace(request.AccessCode)
                ? null
                : await ValidateSignupCodeAsync(request.AccessCode, normalizedOwnerEmail, cancellationToken);
            if (signupCode is not null && request.BusinessSegment != BusinessSegment.Restaurant)
            {
                throw new InvalidOperationException("O codigo de liberacao informado nao e valido para este segmento.");
            }
            if (!Enum.IsDefined(request.BusinessSegment))
            {
                throw new ArgumentException("Segmento invalido para cadastro.", nameof(request.BusinessSegment));
            }

            var tenantIdentifier = await EnsureUniqueTenantIdentifierAsync(
                request.TenantIdentifier ?? request.RestaurantName,
                cancellationToken);

            var tenant = new Tenant(request.RestaurantName, tenantIdentifier);

            var accessSlug = await EnsureUniqueAccessSlugAsync(
                tenant.Id,
                request.AccessSlug ?? request.RestaurantName,
                cancellationToken);

            var company = new Company(
                tenant.Id,
                request.LegalName,
                request.RestaurantName,
                accessSlug,
                contactPhone: request.ContactPhone,
                contactEmail: normalizedOwnerEmail);
            company.ChangeBusinessSegment(request.BusinessSegment);
            if (request.BusinessSegment == BusinessSegment.PetShop)
            {
                company.SetPetShopPublicCode(Convert.ToHexString(
                    System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)).ToLowerInvariant());
            }

            var owner = new AppUser(
                tenant.Id,
                company.Id,
                request.OwnerName,
                normalizedOwnerEmail,
                _passwordHasher.Hash(request.OwnerPassword),
                UserRole.Owner);

            if (signupCode is null)
            {
                owner.Deactivate();
            }

            var selectedPlan = signupCode is not null
                ? CommercialPlanCatalog.Resolve(signupCode.AllowedPlanName)
                : ResolveRequestedPlan(request);
            var maxUsers = signupCode?.AllowedMaxUsers ?? selectedPlan.DefaultMaxUsers;

            var subscription = new Subscription(
                tenant.Id,
                selectedPlan.Name,
                selectedPlan.MonthlyPrice,
                maxUsers,
                DateTime.UtcNow,
                SubscriptionStatus.Active);
            subscription.ApplyCommercialPlan(selectedPlan, maxUsers);

            var qrCodeAccess = request.BusinessSegment == BusinessSegment.Restaurant
                ? new QrCodeAccess(tenant.Id, company.Id, "Entrada principal do restaurante", $"/r/{accessSlug}/menu")
                : null;

            await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            await _tenantRepository.AddAsync(tenant, cancellationToken);
            await _companyRepository.AddAsync(company, cancellationToken);
            await _appUserRepository.AddAsync(owner, cancellationToken);
            await _subscriptionRepository.AddAsync(subscription, cancellationToken);
            if (qrCodeAccess is not null)
            {
                await _qrCodeAccessRepository.AddAsync(qrCodeAccess, cancellationToken);
            }
            signupCode?.RegisterUse(DateTime.UtcNow, normalizedOwnerEmail);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            if (signupCode is null)
            {
                await NotifyPendingApprovalAsync(request, subscription, cancellationToken);
            }
            else
            {
                await _cashOrderTableService.EnsureAsync(tenant.Id, company.Id, cancellationToken);
            }

            return BuildResponse(tenant, company, owner, subscription, selectedPlan.Key, signupCode is null);
        }
        finally
        {
            signupLock.Release();
            if (signupLock.CurrentCount == 1)
            {
                SignupLocks.TryRemove(lockKey, out _);
            }
        }
    }

    private async Task<RestaurantOnboardingResponseDto?> FindExistingSignupAsync(string ownerEmail, CancellationToken cancellationToken)
    {
        var owner = await _context.Users
            .AsNoTracking()
            .Include(item => item.Company)
            .Include(item => item.Tenant)
            .Where(item => item.Role == UserRole.Owner && item.Email == ownerEmail && item.Company.IsActive)
            .OrderByDescending(item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (owner is null)
        {
            return null;
        }

        var subscription = await _context.Subscriptions
            .AsNoTracking()
            .Where(item => item.TenantId == owner.TenantId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new RestaurantOnboardingResponseDto
        {
            TenantIdentifier = owner.Tenant.Identifier,
            AccessSlug = owner.Company.AccessSlug,
            AccessUrl = $"/r/{owner.Company.AccessSlug}/menu",
            OwnerEmail = owner.Email,
            PlanName = subscription?.PlanName ?? string.Empty,
            PlanKey = CommercialPlanCatalog.TryResolve(subscription?.PlanName, out var plan) ? plan.Key : string.Empty,
            BusinessSegment = (int)owner.Company.BusinessSegment,
            RequiresApproval = !owner.IsActive,
            Message = owner.IsActive
                ? "Cadastro ja existe e esta liberado para login."
                : "Pre-cadastro ja recebido. A ZeroPaper vai liberar o login e entrar em contato pelo telefone ou email informado."
        };
    }

    private static RestaurantOnboardingResponseDto BuildResponse(
        Tenant tenant,
        Company company,
        AppUser owner,
        Subscription subscription,
        string planKey,
        bool requiresApproval)
    {
        return new RestaurantOnboardingResponseDto
        {
            TenantIdentifier = tenant.Identifier,
            AccessSlug = company.AccessSlug,
            AccessUrl = $"/r/{company.AccessSlug}/menu",
            OwnerEmail = owner.Email,
            PlanName = subscription.PlanName,
            PlanKey = planKey,
            BusinessSegment = (int)company.BusinessSegment,
            RequiresApproval = requiresApproval,
            Message = requiresApproval
                ? "Pre-cadastro enviado. A ZeroPaper vai liberar o login e entrar em contato pelo telefone ou email informado."
                : "Cadastro liberado."
        };
    }

    private static CommercialPlanDefinition ResolveRequestedPlan(RestaurantOnboardingRequestDto request)
    {
        if (!string.IsNullOrWhiteSpace(request.PlanKey))
        {
            return SegmentCommercialPlanCatalog.Resolve(request.BusinessSegment, request.PlanKey);
        }

        // Compatibilidade com clientes antigos: nome, preco e limite enviados pelo cliente
        // nunca sao usados como fonte de verdade.
        if (request.BusinessSegment != BusinessSegment.Restaurant ||
            !CommercialPlanCatalog.TryResolve(request.PlanName, out var legacyPlan))
        {
            throw new ArgumentException("Informe um plano valido para o segmento selecionado.", nameof(request.PlanKey));
        }

        return legacyPlan;
    }

    private Task<AccessRequestResponseDto> NotifyPendingApprovalAsync(
        RestaurantOnboardingRequestDto request,
        Subscription subscription,
        CancellationToken cancellationToken)
    {
        return _accessRequestNotificationService.SendAsync(new AccessRequestDto
        {
            RestaurantName = request.RestaurantName,
            LegalName = request.LegalName,
            OwnerName = request.OwnerName,
            OwnerEmail = request.OwnerEmail,
            ContactPhone = request.ContactPhone,
            Notes = $"Pre-cadastro criado e aguardando liberacao no painel admin. Plano escolhido: {subscription.PlanName} ({subscription.MonthlyPrice:C}/mes)."
        }, cancellationToken);
    }

    private async Task<SignupCode> ValidateSignupCodeAsync(string rawCode, string ownerEmail, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawCode);

        var normalizedHash = ComputeSignupCodeHash(rawCode);
        var signupCode = await _context.SignupCodes
            .FirstOrDefaultAsync(item => item.CodeHash == normalizedHash, cancellationToken)
            ?? throw new InvalidOperationException("Codigo de liberacao invalido ou expirado.");

        if (!signupCode.IsAvailable(ownerEmail))
        {
            throw new InvalidOperationException("Codigo de liberacao invalido ou expirado.");
        }

        return signupCode;
    }

    private async Task<string> EnsureUniqueTenantIdentifierAsync(string baseIdentifier, CancellationToken cancellationToken)
    {
        var sanitizedBase = Slugify(baseIdentifier);
        var candidate = sanitizedBase;
        var suffix = 1;

        while (await _tenantRepository.IdentifierExistsAsync(candidate, cancellationToken))
        {
            candidate = $"{sanitizedBase}-{suffix++}";
        }

        return candidate;
    }

    private async Task<string> EnsureUniqueAccessSlugAsync(Guid tenantId, string baseSlug, CancellationToken cancellationToken)
    {
        var sanitizedBase = Slugify(baseSlug);
        var candidate = sanitizedBase;
        var suffix = 1;

        while (await _companyRepository.AccessSlugExistsAsync(tenantId, candidate, cancellationToken))
        {
            candidate = $"{sanitizedBase}-{suffix++}";
        }

        return candidate;
    }

    private static string Slugify(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var normalized = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);

            if (unicodeCategory == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(character) ? character : '-');
        }

        var collapsed = builder.ToString();

        while (collapsed.Contains("--", StringComparison.Ordinal))
        {
            collapsed = collapsed.Replace("--", "-", StringComparison.Ordinal);
        }

        var slug = collapsed.Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "tenant" : slug;
    }

    private static string ComputeSignupCodeHash(string rawCode)
    {
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            Encoding.UTF8.GetBytes(SignupCode.NormalizeRawCode(rawCode))));
    }

    private static string NormalizeEmail(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return email.Trim().ToLowerInvariant();
    }

}
