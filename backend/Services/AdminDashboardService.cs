using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.Domain.Plans;
using ZeroPaper.DTOs.Admin;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private static readonly TimeZoneInfo BusinessTimeZone = ResolveBusinessTimeZone();
    private static readonly char[] MasterPasswordChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789".ToCharArray();

    private readonly ZeroPaperDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDataProtector _dataProtector;
    private readonly OpenAiApiOptions _options;

    public AdminDashboardService(
        ZeroPaperDbContext context,
        IPasswordHasher passwordHasher,
        IDataProtectionProvider dataProtectionProvider,
        IOptions<OpenAiApiOptions> options)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _dataProtector = dataProtectionProvider.CreateProtector("ZeroPaper.Admin.MasterPassword.v1");
        _options = options.Value;
    }

    public async Task<AdminDashboardDto> GetDashboardAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        EnsureRoot(session);

        var utcNow = DateTime.UtcNow;
        var onlineThreshold = utcNow.AddMinutes(-10);
        var (dayStartUtc, dayEndUtc) = GetCurrentBusinessDayRangeUtc();
        var aiConfigured = IsAiApiConfigured();

        var codes = await _context.SignupCodes
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(120)
            .Select(item => new SignupCodeDto
            {
                Id = item.Id,
                Label = item.Label,
                BoundEmail = item.BoundEmail,
                AllowedPlanName = item.AllowedPlanName,
                AllowedMaxUsers = item.AllowedMaxUsers,
                ExpiresAtUtc = item.ExpiresAtUtc,
                MaxUses = item.MaxUses,
                UsedCount = item.UsedCount,
                IsActive = item.IsActive,
                CreatedAtUtc = item.CreatedAtUtc,
                LastUsedAtUtc = item.LastUsedAtUtc
            })
            .ToListAsync(cancellationToken);

        var users = await _context.Users
            .AsNoTracking()
            .Where(item => item.Role == UserRole.Root || item.Company.IsActive)
            .OrderBy(item => item.Role == UserRole.Root ? 0 : 1)
            .ThenBy(item => item.Company.TradeName)
            .ThenBy(item => item.FullName)
            .Select(item => new AdminUserDto
            {
                Id = item.Id,
                FullName = item.FullName,
                Email = item.Email,
                Role = item.Role.ToString(),
                RestaurantName = item.Company.TradeName,
                IsActive = item.IsActive,
                IsCompanyActive = item.Company.IsActive,
                ActiveSessionCount = item.Sessions.Count(sessionItem =>
                    sessionItem.IsActive &&
                    sessionItem.RevokedAtUtc == null &&
                    sessionItem.ExpiresAtUtc > utcNow),
                HasActiveSession = item.Sessions.Any(sessionItem =>
                    sessionItem.IsActive &&
                    sessionItem.RevokedAtUtc == null &&
                    sessionItem.ExpiresAtUtc > utcNow),
                IsOnlineNow = item.Sessions.Any(sessionItem =>
                    sessionItem.IsActive &&
                    sessionItem.RevokedAtUtc == null &&
                    sessionItem.ExpiresAtUtc > utcNow &&
                    (sessionItem.LastSeenAtUtc ?? sessionItem.CreatedAtUtc) >= onlineThreshold),
                LastLoginAtUtc = item.LastLoginAtUtc,
                LastSeenAtUtc = item.Sessions
                    .Where(sessionItem =>
                        sessionItem.IsActive &&
                        sessionItem.RevokedAtUtc == null &&
                        sessionItem.ExpiresAtUtc > utcNow)
                    .OrderByDescending(sessionItem => sessionItem.LastSeenAtUtc ?? sessionItem.CreatedAtUtc)
                    .Select(sessionItem => (DateTime?)(sessionItem.LastSeenAtUtc ?? sessionItem.CreatedAtUtc))
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var companies = await _context.Companies
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.TradeName)
            .Select(item => new AdminCompanyFlowDto
            {
                CompanyId = item.Id,
                RestaurantName = item.TradeName,
                AccessSlug = item.AccessSlug,
                OwnerName = item.Users
                    .Where(user => user.Role == UserRole.Owner)
                    .OrderBy(user => user.CreatedAtUtc)
                    .Select(user => user.FullName)
                    .FirstOrDefault() ?? "Owner nao configurado",
                OwnerEmail = item.Users
                    .Where(user => user.Role == UserRole.Owner)
                    .OrderBy(user => user.CreatedAtUtc)
                    .Select(user => user.Email)
                    .FirstOrDefault() ?? string.Empty,
                ContactPhone = item.ContactPhone,
                PlanName = _context.Subscriptions
                    .Where(subscription => subscription.TenantId == item.TenantId && subscription.IsActive)
                    .OrderByDescending(subscription => subscription.StartsAtUtc)
                    .Select(subscription => subscription.PlanName)
                    .FirstOrDefault() ?? "Plano nao informado",
                MonthlyPrice = _context.Subscriptions
                    .Where(subscription => subscription.TenantId == item.TenantId && subscription.IsActive)
                    .OrderByDescending(subscription => subscription.StartsAtUtc)
                    .Select(subscription => (decimal?)subscription.MonthlyPrice)
                    .FirstOrDefault() ?? 0m,
                MaxUsers = _context.Subscriptions
                    .Where(subscription => subscription.TenantId == item.TenantId && subscription.IsActive)
                    .OrderByDescending(subscription => subscription.StartsAtUtc)
                    .Select(subscription => (int?)subscription.MaxUsers)
                    .FirstOrDefault() ?? 1,
                IncludesMenuModule = _context.Subscriptions
                    .Where(subscription => subscription.TenantId == item.TenantId && subscription.IsActive)
                    .OrderByDescending(subscription => subscription.StartsAtUtc)
                    .Select(subscription => (bool?)subscription.IncludesMenuModule)
                    .FirstOrDefault() ?? true,
                IncludesTablesModule = _context.Subscriptions
                    .Where(subscription => subscription.TenantId == item.TenantId && subscription.IsActive)
                    .OrderByDescending(subscription => subscription.StartsAtUtc)
                    .Select(subscription => (bool?)subscription.IncludesTablesModule)
                    .FirstOrDefault() ?? true,
                IncludesKitchenModule = _context.Subscriptions
                    .Where(subscription => subscription.TenantId == item.TenantId && subscription.IsActive)
                    .OrderByDescending(subscription => subscription.StartsAtUtc)
                    .Select(subscription => (bool?)subscription.IncludesKitchenModule)
                    .FirstOrDefault() ?? true,
                IncludesCashModule = _context.Subscriptions
                    .Where(subscription => subscription.TenantId == item.TenantId && subscription.IsActive)
                    .OrderByDescending(subscription => subscription.StartsAtUtc)
                    .Select(subscription => (bool?)subscription.IncludesCashModule)
                    .FirstOrDefault() ?? true,
                IncludesStockModule = _context.Subscriptions
                    .Where(subscription => subscription.TenantId == item.TenantId && subscription.IsActive)
                    .OrderByDescending(subscription => subscription.StartsAtUtc)
                    .Select(subscription => (bool?)subscription.IncludesStockModule)
                    .FirstOrDefault() ?? true,
                IncludesDeliveryModule = _context.Subscriptions
                    .Where(subscription => subscription.TenantId == item.TenantId && subscription.IsActive)
                    .OrderByDescending(subscription => subscription.StartsAtUtc)
                    .Select(subscription => (bool?)subscription.IncludesDeliveryModule)
                    .FirstOrDefault() ?? true,
                IncludesPrintingModule = _context.Subscriptions
                    .Where(subscription => subscription.TenantId == item.TenantId && subscription.IsActive)
                    .OrderByDescending(subscription => subscription.StartsAtUtc)
                    .Select(subscription => (bool?)subscription.IncludesPrintingModule)
                    .FirstOrDefault() ?? true,
                IncludesWaiterCallModule = _context.Subscriptions
                    .Where(subscription => subscription.TenantId == item.TenantId && subscription.IsActive)
                    .OrderByDescending(subscription => subscription.StartsAtUtc)
                    .Select(subscription => (bool?)subscription.IncludesWaiterCallModule)
                    .FirstOrDefault() ?? true,
                IncludesAiAssistantModule = _context.Subscriptions
                    .Where(subscription => subscription.TenantId == item.TenantId && subscription.IsActive)
                    .OrderByDescending(subscription => subscription.StartsAtUtc)
                    .Select(subscription => (bool?)subscription.IncludesAiAssistantModule)
                    .FirstOrDefault() ?? false,
                IsCompanyActive = item.IsActive,
                TablesCount = item.Tables.Count(table => table.IsActive && !table.IsDeliveryChannel),
                MenuItemsCount = item.MenuItems.Count(menuItem => menuItem.IsActive),
                StockItemsCount = item.StockItems.Count(stockItem => stockItem.IsActive),
                TeamMembersCount = item.Users.Count(user => user.IsActive && user.Role != UserRole.Root),
                DeliveryEnabled = item.Tables.Any(table => table.IsActive && table.IsDeliveryChannel),
                AiEnabled = item.EnableAiAssistant,
                AiConfigured = aiConfigured,
                AiModel = item.AiAssistantModel,
                HasMasterPassword = item.AdminMasterPasswordHash != null && item.AdminMasterPasswordCipherText != null,
                MasterPasswordRotatedAtUtc = item.AdminMasterPasswordRotatedAtUtc
            })
            .ToListAsync(cancellationToken);

        foreach (var company in companies)
        {
            ApplyCommercialPlanDisplay(company);
        }

        var orderMetrics = await _context.CustomerOrders
            .AsNoTracking()
            .Where(item => item.IsActive)
            .GroupBy(item => item.CompanyId)
            .Select(group => new
            {
                CompanyId = group.Key,
                OrdersToday = group.Count(item => item.SubmittedAtUtc >= dayStartUtc && item.SubmittedAtUtc < dayEndUtc),
                DeliveryOrdersToday = group.Count(item =>
                    item.SubmittedAtUtc >= dayStartUtc &&
                    item.SubmittedAtUtc < dayEndUtc &&
                    item.DiningTable.IsDeliveryChannel),
                PaidOrdersToday = group.Count(item =>
                    item.PaidAtUtc.HasValue &&
                    item.PaidAtUtc.Value >= dayStartUtc &&
                    item.PaidAtUtc.Value < dayEndUtc),
                OpenOrders = group.Count(item =>
                    item.Status != OrderStatus.Cancelled &&
                    !(item.Status == OrderStatus.Delivered && item.PaymentStatus == PaymentStatus.Paid)),
                PendingPayments = group.Count(item =>
                    item.Status != OrderStatus.Cancelled &&
                    item.PaymentStatus != PaymentStatus.Paid),
                FailedPrints = group.Count(item => item.PrintStatus == PrintStatus.Failed),
                PrintedToday = group.Count(item =>
                    item.PrintedAtUtc.HasValue &&
                    item.PrintedAtUtc.Value >= dayStartUtc &&
                    item.PrintedAtUtc.Value < dayEndUtc),
                LastOrderAtUtc = group.Max(item => (DateTime?)item.SubmittedAtUtc)
            })
            .ToDictionaryAsync(item => item.CompanyId, cancellationToken);

        var deletedMetrics = await _context.DeletedOrderRecords
            .AsNoTracking()
            .Where(item => item.DeletedAtUtc >= dayStartUtc && item.DeletedAtUtc < dayEndUtc)
            .GroupBy(item => item.CompanyId)
            .Select(group => new
            {
                CompanyId = group.Key,
                DeletedOrdersToday = group.Count()
            })
            .ToDictionaryAsync(item => item.CompanyId, cancellationToken);

        var aiMetrics = await _context.AiAssistantInteractions
            .AsNoTracking()
            .Where(item => item.CreatedAtUtc >= dayStartUtc && item.CreatedAtUtc < dayEndUtc)
            .GroupBy(item => item.CompanyId)
            .Select(group => new
            {
                CompanyId = group.Key,
                InteractionsToday = group.Count(),
                SuccessfulInteractionsToday = group.Count(item => item.Succeeded)
            })
            .ToDictionaryAsync(item => item.CompanyId, cancellationToken);

        foreach (var company in companies)
        {
            if (orderMetrics.TryGetValue(company.CompanyId, out var orderMetric))
            {
                company.OrdersToday = orderMetric.OrdersToday;
                company.DeliveryOrdersToday = orderMetric.DeliveryOrdersToday;
                company.PaidOrdersToday = orderMetric.PaidOrdersToday;
                company.OpenOrders = orderMetric.OpenOrders;
                company.PendingPayments = orderMetric.PendingPayments;
                company.FailedPrints = orderMetric.FailedPrints;
                company.PrintedToday = orderMetric.PrintedToday;
                company.LastOrderAtUtc = orderMetric.LastOrderAtUtc;
            }

            if (deletedMetrics.TryGetValue(company.CompanyId, out var deletedMetric))
            {
                company.DeletedOrdersToday = deletedMetric.DeletedOrdersToday;
            }

            if (aiMetrics.TryGetValue(company.CompanyId, out var aiMetric))
            {
                company.AiInteractionsToday = aiMetric.InteractionsToday;
                company.SuccessfulAiInteractionsToday = aiMetric.SuccessfulInteractionsToday;
            }
        }

        var summary = new AdminDashboardSummaryDto
        {
            TotalCompanies = companies.Count,
            ActiveCompanies = companies.Count(item => item.IsCompanyActive),
            TotalUsers = users.Count,
            OnlineUsers = users.Count(item => item.IsOnlineNow),
            AvailableSignupCodes = codes.Count(IsCodeAvailable),
            UsedSignupCodes = codes.Count(item => !item.IsActive && item.UsedCount > 0),
            ExpiredSignupCodes = codes.Count(item => item.ExpiresAtUtc <= utcNow),
            OrdersToday = companies.Sum(item => item.OrdersToday),
            OpenOrders = companies.Sum(item => item.OpenOrders),
            PendingPayments = companies.Sum(item => item.PendingPayments),
            FailedPrints = companies.Sum(item => item.FailedPrints),
            AiInteractionsToday = companies.Sum(item => item.AiInteractionsToday)
        };

        return new AdminDashboardDto
        {
            Summary = summary,
            Codes = codes,
            Users = users,
            Companies = companies
        };
    }

    public async Task<AdminCompanyPlanUpdateDto> UpdateCompanyPlanAsync(
        WorkspaceSessionContext session,
        Guid companyId,
        UpdateAdminCompanyPlanRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Password);

        EnsureRoot(session);
        await ValidateRootPasswordAsync(session, request.Password, cancellationToken);

        if (request.MaxUsers <= 0)
        {
            throw new InvalidOperationException("O limite de usuarios precisa ser maior que zero.");
        }

        var hasStandardPlanRequest = CommercialPlanCatalog.TryResolve(request.PlanName, out var requestedPlan);

        if (!hasStandardPlanRequest && !HasAnyEnabledModule(request))
        {
            throw new InvalidOperationException("Selecione pelo menos um modulo para esse plano.");
        }

        var company = await _context.Companies
            .FirstOrDefaultAsync(item => item.Id == companyId, cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");

        var subscription = await _context.Subscriptions
            .Where(item => item.TenantId == company.TenantId && item.IsActive)
            .OrderByDescending(item => item.StartsAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Assinatura ativa nao encontrada.");

        var activeNonRootUsers = await _context.Users.CountAsync(
            item => item.CompanyId == company.Id && item.IsActive && item.Role != UserRole.Root,
            cancellationToken);

        if (request.MaxUsers < activeNonRootUsers)
        {
            throw new InvalidOperationException($"O plano precisa comportar pelo menos {activeNonRootUsers} conta(s) ativa(s) dessa unidade.");
        }

        CommercialPlanFeatures planFeatures;

        if (hasStandardPlanRequest)
        {
            subscription.ApplyCommercialPlan(requestedPlan, request.MaxUsers);
            planFeatures = requestedPlan.Features;
        }
        else
        {
            subscription.UpdateFeatureSet(
                request.IncludesMenuModule,
                request.IncludesTablesModule,
                request.IncludesKitchenModule,
                request.IncludesCashModule,
                request.IncludesStockModule,
                request.IncludesDeliveryModule,
                request.IncludesPrintingModule,
                request.IncludesWaiterCallModule,
                request.IncludesAiAssistantModule);

            var resolvedPlan = ResolvePlanFromFeatureSet(subscription);
            if (resolvedPlan is not null)
            {
                subscription.ApplyCommercialPlan(resolvedPlan, request.MaxUsers);
                planFeatures = resolvedPlan.Features;
            }
            else
            {
                var monthlyPrice = CalculatePlanPrice(subscription);
                subscription.ChangePlan(CommercialPlanCatalog.CustomPlanName, monthlyPrice, request.MaxUsers);
                planFeatures = CommercialPlanCatalog.ResolveFeatures(subscription.PlanName);
            }
        }

        if (!subscription.IncludesAiAssistantModule && company.EnableAiAssistant)
        {
            company.UpdateAiAssistantSettings(
                false,
                company.AiAssistantModel,
                company.AiAssistantSystemPrompt,
                company.AiAssistantGreetingMessage,
                company.AiAssistantRedirectMessage,
                company.AiAssistantFallbackMessage,
                company.AiAssistantOrderingLink,
                company.AiAssistantPixReceiverName,
                company.AiAssistantPixKey,
                company.AiAssistantPixMessage,
                company.AiAssistantServiceDays,
                company.AiAssistantServiceStartTime,
                company.AiAssistantServiceEndTime,
                company.AiAssistantMaxOutputTokens);
        }

        if (!planFeatures.HasAutoPrint && company.EnableAutomaticPrinting)
        {
            company.UpdatePrintingPreferences(
                false,
                company.PrintPaperProfile,
                company.PrintOrdersPerPage);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return MapPlanUpdate(company, subscription);
    }

    public async Task DeleteCompanyAsync(
        WorkspaceSessionContext session,
        Guid companyId,
        DeleteAdminCompanyRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Password);

        EnsureRoot(session);
        await ValidateRootPasswordAsync(session, request.Password, cancellationToken);

        var company = await _context.Companies
            .FirstOrDefaultAsync(item => item.Id == companyId, cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");

        if (company.Id == session.CompanyId)
        {
            throw new InvalidOperationException("Nao e possivel apagar a unidade usada pela sua sessao root atual.");
        }

        if (!string.Equals(
                NormalizeCompanyDeleteConfirmation(request.ConfirmationText),
                NormalizeCompanyDeleteConfirmation(company.TradeName),
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Digite exatamente {company.TradeName} para confirmar.");
        }

        var utcNow = DateTime.UtcNow;

        var users = await _context.Users
            .Where(item => item.CompanyId == company.Id && item.Role != UserRole.Root)
            .ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            user.Deactivate();
        }

        var sessions = await _context.Sessions
            .Where(item =>
                item.CompanyId == company.Id &&
                item.IsActive &&
                item.RevokedAtUtc == null &&
                item.ExpiresAtUtc > utcNow)
            .ToListAsync(cancellationToken);

        foreach (var appSession in sessions)
        {
            appSession.Revoke(utcNow);
        }

        var subscriptions = await _context.Subscriptions
            .Where(item => item.TenantId == company.TenantId && item.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var subscription in subscriptions)
        {
            subscription.Cancel(subscription.StartsAtUtc > utcNow ? subscription.StartsAtUtc : utcNow);
            subscription.Deactivate();
        }

        company.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeCompanyDeleteConfirmation(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    public async Task<AdminCompanyMasterPasswordRevealDto> RevealCompanyMasterPasswordAsync(
        WorkspaceSessionContext session,
        Guid companyId,
        AdminSensitiveActionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Password);

        EnsureRoot(session);
        await ValidateRootPasswordAsync(session, request.Password, cancellationToken);

        var company = await _context.Companies
            .FirstOrDefaultAsync(item => item.Id == companyId, cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");

        var rawPassword = await EnsureCompanyMasterPasswordAsync(company, rotate: false, cancellationToken);
        return BuildMasterPasswordReveal(company, rawPassword);
    }

    public async Task<AdminCompanyMasterPasswordRevealDto> RotateCompanyMasterPasswordAsync(
        WorkspaceSessionContext session,
        Guid companyId,
        AdminSensitiveActionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Password);

        EnsureRoot(session);
        await ValidateRootPasswordAsync(session, request.Password, cancellationToken);

        var company = await _context.Companies
            .FirstOrDefaultAsync(item => item.Id == companyId, cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");

        var rawPassword = await EnsureCompanyMasterPasswordAsync(company, rotate: true, cancellationToken);
        return BuildMasterPasswordReveal(company, rawPassword);
    }

    private async Task<string> EnsureCompanyMasterPasswordAsync(Company company, bool rotate, CancellationToken cancellationToken)
    {
        if (!rotate &&
            !string.IsNullOrWhiteSpace(company.AdminMasterPasswordHash) &&
            !string.IsNullOrWhiteSpace(company.AdminMasterPasswordCipherText))
        {
            try
            {
                return _dataProtector.Unprotect(company.AdminMasterPasswordCipherText);
            }
            catch (CryptographicException)
            {
            }
        }

        var rawPassword = GenerateMasterPassword();
        company.UpdateAdminMasterPassword(
            _passwordHasher.Hash(rawPassword),
            _dataProtector.Protect(rawPassword),
            DateTime.UtcNow);

        await _context.SaveChangesAsync(cancellationToken);
        return rawPassword;
    }

    private async Task ValidateRootPasswordAsync(
        WorkspaceSessionContext session,
        string password,
        CancellationToken cancellationToken)
    {
        var currentUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == session.UserId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Conta root nao encontrada.");

        if (currentUser.Role != UserRole.Root || !_passwordHasher.Verify(password, currentUser.PasswordHash))
        {
            throw new InvalidOperationException("Senha root incorreta.");
        }
    }

    private static bool IsCodeAvailable(SignupCodeDto code)
    {
        return code.IsActive && code.ExpiresAtUtc > DateTime.UtcNow && code.UsedCount < code.MaxUses;
    }

    private AdminCompanyMasterPasswordRevealDto BuildMasterPasswordReveal(Company company, string rawPassword)
    {
        return new AdminCompanyMasterPasswordRevealDto
        {
            CompanyId = company.Id,
            RestaurantName = company.TradeName,
            HasMasterPassword = true,
            MaskedPassword = MaskPasswordValue(rawPassword),
            RotatedAtUtc = company.AdminMasterPasswordRotatedAtUtc,
            RawPassword = rawPassword
        };
    }

    private static AdminCompanyPlanUpdateDto MapPlanUpdate(Company company, Subscription subscription)
    {
        var result = new AdminCompanyPlanUpdateDto
        {
            CompanyId = company.Id,
            RestaurantName = company.TradeName,
            PlanName = subscription.PlanName,
            PlanTier = CommercialPlanCatalog.ResolveTier(subscription.PlanName).ToString(),
            MonthlyPrice = subscription.MonthlyPrice,
            MaxUsers = subscription.MaxUsers,
            IncludesMenuModule = subscription.IncludesMenuModule,
            IncludesTablesModule = subscription.IncludesTablesModule,
            IncludesKitchenModule = subscription.IncludesKitchenModule,
            IncludesCashModule = subscription.IncludesCashModule,
            IncludesStockModule = subscription.IncludesStockModule,
            IncludesDeliveryModule = subscription.IncludesDeliveryModule,
            IncludesPrintingModule = subscription.IncludesPrintingModule,
            IncludesWaiterCallModule = subscription.IncludesWaiterCallModule,
            IncludesAiAssistantModule = subscription.IncludesAiAssistantModule,
        };

        ApplyCommercialPlanFlags(result);
        return result;
    }

    private static decimal CalculatePlanPrice(Subscription subscription)
    {
        var total = 0m;

        if (subscription.IncludesMenuModule) total += Subscription.MenuModulePrice;
        if (subscription.IncludesTablesModule) total += Subscription.TablesModulePrice;
        if (subscription.IncludesKitchenModule) total += Subscription.KitchenModulePrice;
        if (subscription.IncludesCashModule) total += Subscription.CashModulePrice;
        if (subscription.IncludesStockModule) total += Subscription.StockModulePrice;
        if (subscription.IncludesDeliveryModule) total += Subscription.DeliveryModulePrice;
        if (subscription.IncludesPrintingModule) total += Subscription.PrintingModulePrice;
        if (subscription.IncludesWaiterCallModule) total += Subscription.WaiterCallModulePrice;
        if (subscription.IncludesAiAssistantModule) total += Subscription.AiAssistantModulePrice;

        return decimal.Round(total, 2);
    }

    private static CommercialPlanDefinition? ResolvePlanFromFeatureSet(Subscription subscription)
    {
        foreach (var plan in CommercialPlanCatalog.StandardPlans)
        {
            if (subscription.IncludesMenuModule == plan.IncludesMenuModule &&
                subscription.IncludesTablesModule == plan.IncludesTablesModule &&
                subscription.IncludesKitchenModule == plan.IncludesKitchenModule &&
                subscription.IncludesCashModule == plan.IncludesCashModule &&
                subscription.IncludesStockModule == plan.IncludesStockModule &&
                subscription.IncludesDeliveryModule == plan.IncludesDeliveryModule &&
                subscription.IncludesPrintingModule == plan.IncludesPrintingModule &&
                subscription.IncludesWaiterCallModule == plan.IncludesWaiterCallModule &&
                subscription.IncludesAiAssistantModule == plan.IncludesAiAssistantModule)
            {
                return plan;
            }
        }

        return null;
    }

    private static void ApplyCommercialPlanDisplay(AdminCompanyFlowDto company)
    {
        if (CommercialPlanCatalog.TryResolve(company.PlanName, out var plan))
        {
            company.PlanName = plan.Name;
            company.MonthlyPrice = plan.MonthlyPrice;
        }

        company.PlanTier = CommercialPlanCatalog.ResolveTier(company.PlanName).ToString();
        ApplyCommercialPlanFlags(company);
    }

    private static void ApplyCommercialPlanFlags(AdminCompanyFlowDto company)
    {
        var features = CommercialPlanCatalog.ResolveFeatures(company.PlanName);
        company.HasWhatsAppAI = features.HasWhatsAppAI;
        company.HasDelivery = features.HasDelivery;
        company.HasAutoPrint = features.HasAutoPrint;
        company.HasBasicReports = features.HasBasicReports;
        company.HasManagementDashboard = features.HasManagementDashboard;
        company.HasAdvancedReports = features.HasAdvancedReports;
        company.HasCoupons = features.HasCoupons;
        company.HasRecurringCustomers = features.HasRecurringCustomers;
    }

    private static void ApplyCommercialPlanFlags(AdminCompanyPlanUpdateDto planUpdate)
    {
        var features = CommercialPlanCatalog.ResolveFeatures(planUpdate.PlanName);
        planUpdate.HasWhatsAppAI = features.HasWhatsAppAI;
        planUpdate.HasDelivery = features.HasDelivery;
        planUpdate.HasAutoPrint = features.HasAutoPrint;
        planUpdate.HasBasicReports = features.HasBasicReports;
        planUpdate.HasManagementDashboard = features.HasManagementDashboard;
        planUpdate.HasAdvancedReports = features.HasAdvancedReports;
        planUpdate.HasCoupons = features.HasCoupons;
        planUpdate.HasRecurringCustomers = features.HasRecurringCustomers;
    }

    private bool IsAiApiConfigured()
    {
        return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? _options.ApiKey);
    }

    private static bool HasAnyEnabledModule(UpdateAdminCompanyPlanRequestDto request)
    {
        return request.IncludesMenuModule ||
               request.IncludesTablesModule ||
               request.IncludesKitchenModule ||
               request.IncludesCashModule ||
               request.IncludesStockModule ||
               request.IncludesDeliveryModule ||
               request.IncludesPrintingModule ||
               request.IncludesWaiterCallModule ||
               request.IncludesAiAssistantModule;
    }

    private static string GenerateMasterPassword()
    {
        const int rawLength = 20;
        Span<char> raw = stackalloc char[rawLength];
        var randomBytes = RandomNumberGenerator.GetBytes(rawLength);

        for (var index = 0; index < rawLength; index++)
        {
            raw[index] = MasterPasswordChars[randomBytes[index] % MasterPasswordChars.Length];
        }

        return $"ZP-{new string(raw[..4])}-{new string(raw[4..8])}-{new string(raw[8..12])}-{new string(raw[12..16])}-{new string(raw[16..20])}";
    }

    private static string MaskPasswordValue(string rawPassword)
    {
        if (string.IsNullOrWhiteSpace(rawPassword))
        {
            return "Oculta";
        }

        var visibleTail = rawPassword.Length >= 4 ? rawPassword[^4..] : rawPassword;
        return $"************{visibleTail}";
    }

    private static string MaskPassword(string rawPassword)
    {
        if (string.IsNullOrWhiteSpace(rawPassword))
        {
            return "Oculta";
        }

        var visibleTail = rawPassword.Length >= 4 ? rawPassword[^4..] : rawPassword;
        return $"••••••••••••{visibleTail}";
    }

    private static void EnsureRoot(WorkspaceSessionContext session)
    {
        if (!Enum.TryParse<UserRole>(session.Role, true, out var role) || role != UserRole.Root)
        {
            throw new UnauthorizedAccessException("Root access is required.");
        }
    }

    private static (DateTime dayStartUtc, DateTime dayEndUtc) GetCurrentBusinessDayRangeUtc()
    {
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BusinessTimeZone);
        var dayStartLocal = new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var dayEndLocal = dayStartLocal.AddDays(1);
        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(dayStartLocal, BusinessTimeZone);
        var dayEndUtc = TimeZoneInfo.ConvertTimeToUtc(dayEndLocal, BusinessTimeZone);
        return (dayStartUtc, dayEndUtc);
    }

    private static TimeZoneInfo ResolveBusinessTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
