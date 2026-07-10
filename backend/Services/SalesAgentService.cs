using ZeroPaper.Domain.Entities;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Repositories.Interfaces;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class SalesAgentService : ISalesAgentService
{
    private readonly ISalesAgentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkspaceService _workspaceService;
    private readonly ICashOrderTableService _cashOrderTableService;

    public SalesAgentService(
        ISalesAgentRepository repository,
        IUnitOfWork unitOfWork,
        IWorkspaceService workspaceService,
        ICashOrderTableService cashOrderTableService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _workspaceService = workspaceService;
        _cashOrderTableService = cashOrderTableService;
    }

    public async Task<IReadOnlyList<SalesAgentDto>> GetByCompanyAsync(
        WorkspaceSessionContext session,
        CancellationToken cancellationToken = default)
    {
        var agents = await _repository.GetByCompanyAsync(session.CompanyId, includeInactive: true, cancellationToken);
        return agents.Select(MapToDto).ToList();
    }

    public async Task<SalesAgentDto> CreateAsync(
        WorkspaceSessionContext session,
        CreateSalesAgentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        if (request.CommissionPercent.HasValue && (request.CommissionPercent < 0 || request.CommissionPercent > 100))
        {
            throw new ArgumentException("Comissao deve estar entre 0 e 100.", nameof(request.CommissionPercent));
        }

        var agent = new SalesAgent(
            session.TenantId,
            session.CompanyId,
            request.Name,
            request.Phone,
            request.CommissionPercent);

        await _repository.AddAsync(agent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(agent);
    }

    public async Task<SalesAgentDto> UpdateAsync(
        WorkspaceSessionContext session,
        Guid agentId,
        UpdateSalesAgentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        var agent = await _repository.GetByIdAsync(session.CompanyId, agentId, cancellationToken)
            ?? throw new KeyNotFoundException("Vendedor nao encontrado.");

        if (request.CommissionPercent.HasValue && (request.CommissionPercent < 0 || request.CommissionPercent > 100))
        {
            throw new ArgumentException("Comissao deve estar entre 0 e 100.", nameof(request.CommissionPercent));
        }

        agent.UpdateInfo(request.Name, request.Phone, request.CommissionPercent);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(agent);
    }

    public async Task<SalesAgentDto> UpdateStatusAsync(
        WorkspaceSessionContext session,
        Guid agentId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var agent = await _repository.GetByIdAsync(session.CompanyId, agentId, cancellationToken)
            ?? throw new KeyNotFoundException("Vendedor nao encontrado.");

        if (isActive)
        {
            agent.Activate();
        }
        else
        {
            agent.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(agent);
    }

    public async Task<PublicSellerLinkDto> GetPublicSellerLinkAsync(string code, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var agent = await _repository.GetByCodeWithCompanyAsync(code.Trim().ToLowerInvariant(), cancellationToken)
            ?? throw new KeyNotFoundException("Link de vendedor nao encontrado ou inativo.");

        var cashTable = await _cashOrderTableService.EnsureAsync(agent.TenantId, agent.CompanyId, cancellationToken);

        return new PublicSellerLinkDto
        {
            SellerName = agent.Name,
            CompanyName = agent.Company.TradeName,
            CompanyLogoUrl = agent.Company.LogoUrl,
            CashTablePublicCode = cashTable.QrCodeAccess.PublicCode
        };
    }

    private static SalesAgentDto MapToDto(SalesAgent agent) => new()
    {
        Id = agent.Id,
        Name = agent.Name,
        Phone = agent.Phone,
        Code = agent.Code,
        CommissionPercent = agent.CommissionPercent,
        IsActive = agent.IsActive,
        CreatedAtUtc = agent.CreatedAtUtc
    };
}
