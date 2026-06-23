using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Services;

public class CashOrderTableService : ICashOrderTableService
{
    private const string CashOrderTableName = "Pedido no caixa";
    private const string CashOrderInternalCode = "PEDIDO_CAIXA";
    private readonly ZeroPaperDbContext _context;

    public CashOrderTableService(ZeroPaperDbContext context)
    {
        _context = context;
    }

    public async Task<DiningTable> EnsureAsync(
        Guid tenantId,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var existingTable = await _context.DiningTables
            .Include(item => item.QrCodeAccess)
            .FirstOrDefaultAsync(
                item => item.CompanyId == companyId &&
                        !item.IsDeliveryChannel &&
                        (item.InternalCode == CashOrderInternalCode ||
                         item.Name.ToLower() == "pedido no caixa" ||
                         item.Name.ToLower() == "pedir no caixa"),
                cancellationToken);

        if (existingTable is not null)
        {
            var changed = false;

            if (!existingTable.IsActive)
            {
                existingTable.Activate();
                existingTable.QrCodeAccess.Activate();
                changed = true;
            }

            if (!string.Equals(existingTable.Name, CashOrderTableName, StringComparison.Ordinal))
            {
                existingTable.Rename(CashOrderTableName);
                changed = true;
            }

            if (!string.Equals(existingTable.InternalCode, CashOrderInternalCode, StringComparison.Ordinal))
            {
                existingTable.ChangeInternalCode(CashOrderInternalCode);
                changed = true;
            }

            if (changed)
            {
                existingTable.QrCodeAccess.UpdateDestination(CashOrderTableName, existingTable.QrCodeAccess.AccessPath);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return existingTable;
        }

        var publicCode = await GenerateUniquePublicCodeAsync(cancellationToken);
        var qrCodeAccess = new QrCodeAccess(
            tenantId,
            companyId,
            CashOrderTableName,
            $"/q/{publicCode}",
            publicCode: publicCode);
        var table = new DiningTable(
            tenantId,
            companyId,
            qrCodeAccess.Id,
            CashOrderTableName,
            CashOrderInternalCode,
            seats: 1,
            comandaLabel: "Caixa");

        await _context.QrCodeAccesses.AddAsync(qrCodeAccess, cancellationToken);
        await _context.DiningTables.AddAsync(table, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return table;
    }

    public async Task EnsureForActiveOwnersAsync(CancellationToken cancellationToken = default)
    {
        var activeOwnerCompanies = await _context.Users
            .AsNoTracking()
            .Where(item =>
                item.Role == UserRole.Owner &&
                item.IsActive &&
                item.Company.IsActive)
            .Select(item => new { item.TenantId, item.CompanyId })
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var company in activeOwnerCompanies)
        {
            await EnsureAsync(company.TenantId, company.CompanyId, cancellationToken);
        }
    }

    private async Task<string> GenerateUniquePublicCodeAsync(CancellationToken cancellationToken)
    {
        string publicCode;

        do
        {
            publicCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();
        }
        while (await _context.QrCodeAccesses.AnyAsync(item => item.PublicCode == publicCode, cancellationToken));

        return publicCode;
    }
}
