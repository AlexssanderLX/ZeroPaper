using System.Data;
using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public sealed class AppointmentService : IAppointmentService
{
    private readonly ZeroPaperDbContext _context;
    private readonly IWorkspaceService? _workspaceService;
    private readonly ICashOrderTableService? _cashOrderTableService;

    public AppointmentService(ZeroPaperDbContext context, IWorkspaceService workspaceService, ICashOrderTableService cashOrderTableService)
    {
        _context = context;
        _workspaceService = workspaceService;
        _cashOrderTableService = cashOrderTableService;
    }

    internal AppointmentService(ZeroPaperDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AppointmentDto>> GetAsync(
        WorkspaceSessionContext session,
        DateTime fromUtc,
        DateTime toUtc,
        AppointmentStatus? status,
        Guid? petId,
        Guid? customerId,
        Guid? assignedUserId,
        CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        ValidateUtcRange(fromUtc, toUtc);

        var query = BaseQuery(tracking: false)
            .Where(item =>
                item.TenantId == session.TenantId &&
                item.CompanyId == session.CompanyId &&
                item.StartsAtUtc >= fromUtc &&
                item.StartsAtUtc < toUtc);
        if (status.HasValue) query = query.Where(item => item.Status == status.Value);
        if (petId.HasValue) query = query.Where(item => item.PetId == petId.Value);
        if (customerId.HasValue) query = query.Where(item => item.Pet.CustomerProfileId == customerId.Value);
        if (assignedUserId.HasValue) query = query.Where(item => item.AssignedUserId == assignedUserId.Value);

        var appointments = await query
            .OrderBy(item => item.StartsAtUtc)
            .ToListAsync(cancellationToken);

        return appointments.Select(MapToDto).ToList();
    }

    public async Task<AppointmentDto> GetByIdAsync(
        WorkspaceSessionContext session,
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        return MapToDto(await GetEntityAsync(session, appointmentId, tracking: false, cancellationToken));
    }

    public async Task<AppointmentDto> CreateAsync(
        WorkspaceSessionContext session,
        CreateAppointmentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        ArgumentNullException.ThrowIfNull(request);

        var pet = await _context.Pets.AsNoTracking().FirstOrDefaultAsync(item =>
            item.Id == request.PetId &&
            item.TenantId == session.TenantId &&
            item.CompanyId == session.CompanyId &&
            item.IsActive,
            cancellationToken) ?? throw new KeyNotFoundException("Animal nao encontrado.");

        var service = await _context.MenuItems.AsNoTracking().FirstOrDefaultAsync(item =>
            item.Id == request.MenuItemId &&
            item.TenantId == session.TenantId &&
            item.CompanyId == session.CompanyId &&
            item.Kind == CatalogItemKind.Service &&
            item.IsActive,
            cancellationToken) ?? throw new KeyNotFoundException("Servico nao encontrado.");

        var duration = request.DurationMinutes ?? service.EstimatedDurationMinutes
            ?? throw new ArgumentException("Informe a duracao do atendimento.", nameof(request.DurationMinutes));

        await ValidateAssignedUserAsync(session, request.AssignedUserId, cancellationToken);

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await EnsureNoConflictAsync(session, null, pet.Id, request.AssignedUserId, request.StartsAtUtc, duration, cancellationToken);

        var appointment = new Appointment(
            session.TenantId,
            session.CompanyId,
            pet.Id,
            service.Id,
            request.StartsAtUtc,
            duration,
            service.Name,
            service.Price,
            request.CustomerNotes);

        appointment.AssignUser(request.AssignedUserId);
        await _context.Appointments.AddAsync(appointment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(session, appointment.Id, cancellationToken);
    }

    public async Task<AppointmentDto> RescheduleAsync(
        WorkspaceSessionContext session,
        Guid appointmentId,
        RescheduleAppointmentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        ArgumentNullException.ThrowIfNull(request);

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var appointment = await GetEntityAsync(session, appointmentId, tracking: true, cancellationToken);
        var previousStart = appointment.StartsAtUtc;
        await EnsureNoConflictAsync(session, appointment.Id, appointment.PetId, appointment.AssignedUserId, request.StartsAtUtc, request.DurationMinutes, cancellationToken);
        appointment.Reschedule(request.StartsAtUtc, request.DurationMinutes);
        await AddHistoryAsync(session, appointment, appointment.Status, appointment.Status, $"Reagendado de {previousStart:O} para {request.StartsAtUtc:O}", cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetByIdAsync(session, appointment.Id, cancellationToken);
    }

    public async Task<AppointmentDto> UpdateNotesAsync(
        WorkspaceSessionContext session,
        Guid appointmentId,
        UpdateAppointmentNotesRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        var appointment = await GetEntityAsync(session, appointmentId, tracking: true, cancellationToken);
        appointment.UpdateNotes(request.CustomerNotes, request.InternalNotes);
        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(session, appointment.Id, cancellationToken);
    }

    public async Task<AppointmentDto> AssignAsync(
        WorkspaceSessionContext session,
        Guid appointmentId,
        Guid? assignedUserId,
        CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        await ValidateAssignedUserAsync(session, assignedUserId, cancellationToken);
        var appointment = await GetEntityAsync(session, appointmentId, tracking: true, cancellationToken);
        await EnsureNoConflictAsync(session, appointment.Id, appointment.PetId, assignedUserId, appointment.StartsAtUtc, appointment.DurationMinutes, cancellationToken);
        appointment.AssignUser(assignedUserId);
        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(session, appointment.Id, cancellationToken);
    }

    public async Task<AppointmentDto> LinkOrderAsync(
        WorkspaceSessionContext session,
        Guid appointmentId,
        Guid customerOrderId,
        CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        var orderExists = await _context.CustomerOrders.AsNoTracking().AnyAsync(item =>
            item.Id == customerOrderId &&
            item.TenantId == session.TenantId &&
            item.CompanyId == session.CompanyId &&
            item.IsActive,
            cancellationToken);
        if (!orderExists) throw new KeyNotFoundException("Pedido nao encontrado.");

        var appointment = await GetEntityAsync(session, appointmentId, tracking: true, cancellationToken);
        appointment.LinkCustomerOrder(customerOrderId);
        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(session, appointment.Id, cancellationToken);
    }

    public async Task<AppointmentOrderResultDto> CreateOrderAsync(WorkspaceSessionContext session, Guid appointmentId, CreateAppointmentOrderRequestDto request, CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        var appointment = await _context.Appointments.Include(item => item.Pet).ThenInclude(item => item.CustomerProfile)
            .FirstOrDefaultAsync(item => item.Id == appointmentId && item.CompanyId == session.CompanyId && item.TenantId == session.TenantId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Agendamento nao encontrado.");
        if (appointment.CustomerOrderId.HasValue) throw new InvalidOperationException("O agendamento ja possui um pedido vinculado.");
        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow) throw new InvalidOperationException("Nao e possivel cobrar um agendamento cancelado ou ausente.");

        var price = request.UnitPrice ?? appointment.UnitPriceSnapshot;
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(request.UnitPrice));
        var cashOrderTableService = _cashOrderTableService ?? throw new InvalidOperationException("Integracao de caixa indisponivel.");
        var workspaceService = _workspaceService ?? throw new InvalidOperationException("Integracao de pedidos indisponivel.");
        var table = await cashOrderTableService.EnsureAsync(session.TenantId, session.CompanyId, cancellationToken);
        var order = await workspaceService.CreateOrderAsync(session, new CreateCustomerOrderRequestDto
        {
            TableId = table.Id,
            CustomerName = appointment.Pet.CustomerProfile.CustomerName,
            DeliveryPhone = appointment.Pet.CustomerProfile.Phone,
            FulfillmentType = "Local",
            PaymentMethod = request.PaymentMethod,
            Notes = request.Notes ?? $"Agendamento {appointment.Id}",
            Items = [new OrderItemInputDto { Name = appointment.ServiceNameSnapshot, Quantity = 1, UnitPrice = price }]
        }, cancellationToken);
        appointment.LinkCustomerOrder(order.Id);
        await _context.SaveChangesAsync(cancellationToken);
        return new AppointmentOrderResultDto { Appointment = await GetByIdAsync(session, appointment.Id, cancellationToken), Order = order };
    }

    public async Task<AppointmentDto> ChangeStatusAsync(
        WorkspaceSessionContext session,
        Guid appointmentId,
        ChangeAppointmentStatusRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        var appointment = await GetEntityAsync(session, appointmentId, tracking: true, cancellationToken);
        var previousStatus = appointment.Status;
        var now = DateTime.UtcNow;

        switch (request.Status)
        {
            case AppointmentStatus.Confirmed: appointment.Confirm(now); break;
            case AppointmentStatus.InProgress: appointment.Start(now); break;
            case AppointmentStatus.Completed: appointment.Complete(now); break;
            case AppointmentStatus.Cancelled: appointment.Cancel(request.CancellationReason ?? string.Empty, now); break;
            case AppointmentStatus.NoShow: appointment.MarkAsNoShow(now); break;
            default: throw new ArgumentException("A transicao de status solicitada e invalida.", nameof(request.Status));
        }

        await AddHistoryAsync(session, appointment, previousStatus, appointment.Status, request.CancellationReason, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(session, appointment.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<AppointmentHistoryDto>> GetHistoryAsync(WorkspaceSessionContext session, Guid appointmentId, CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        _ = await GetEntityAsync(session, appointmentId, tracking: false, cancellationToken);
        return await _context.AppointmentStatusHistories.AsNoTracking()
            .Where(item => item.AppointmentId == appointmentId && item.CompanyId == session.CompanyId && item.TenantId == session.TenantId)
            .Include(item => item.ChangedByUser).OrderBy(item => item.ChangedAtUtc)
            .Select(item => new AppointmentHistoryDto
            {
                Id = item.Id, PreviousStatus = item.PreviousStatus, NewStatus = item.NewStatus,
                ChangedByUserId = item.ChangedByUserId, ChangedByUserName = item.ChangedByUser.FullName,
                ChangedAtUtc = item.ChangedAtUtc, Reason = item.Reason
            }).ToListAsync(cancellationToken);
    }

    public async Task<AppointmentScheduleSettingsDto> GetSettingsAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        var company = await GetCompanyAsync(session, tracking: false, cancellationToken);
        return MapSettings(company);
    }

    public async Task<AppointmentScheduleSettingsDto> UpdateSettingsAsync(WorkspaceSessionContext session, UpdateAppointmentScheduleSettingsRequestDto request, CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        if (!TimeOnly.TryParseExact(request.StartTime, "HH:mm", out var start) || !TimeOnly.TryParseExact(request.EndTime, "HH:mm", out var end))
            throw new ArgumentException("Use horarios no formato HH:mm.");
        var company = await GetCompanyAsync(session, tracking: true, cancellationToken);
        company.UpdateAppointmentSchedule(request.ServiceDays, start, end, request.SlotIntervalMinutes);
        await _context.SaveChangesAsync(cancellationToken);
        return MapSettings(company);
    }

    public async Task<AppointmentAvailabilityDto> GetAvailabilityAsync(WorkspaceSessionContext session, DateOnly date, Guid serviceId, Guid? assignedUserId, CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        var company = await GetCompanyAsync(session, tracking: false, cancellationToken);
        var service = await _context.MenuItems.AsNoTracking().FirstOrDefaultAsync(item =>
            item.Id == serviceId && item.CompanyId == session.CompanyId && item.TenantId == session.TenantId &&
            item.Kind == CatalogItemKind.Service && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Servico nao encontrado.");
        await ValidateAssignedUserAsync(session, assignedUserId, cancellationToken);
        var duration = service.EstimatedDurationMinutes ?? throw new InvalidOperationException("O servico nao possui duracao estimada.");
        var serviceDays = company.AppointmentServiceDays.Split(',').Select(int.Parse).ToHashSet();
        var result = new AppointmentAvailabilityDto { Date = date, ServiceId = service.Id, DurationMinutes = duration, TimeZone = company.TimeZoneId };
        if (!serviceDays.Contains((int)date.DayOfWeek)) return result;

        var timezone = TimeZoneInfo.FindSystemTimeZoneById(company.TimeZoneId);
        var localStart = date.ToDateTime(company.AppointmentStartTime, DateTimeKind.Unspecified);
        var localEnd = date.ToDateTime(company.AppointmentEndTime, DateTimeKind.Unspecified);
        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, timezone);
        var dayEndUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, timezone);
        var appointments = await _context.Appointments.AsNoTracking().Where(item =>
            item.CompanyId == session.CompanyId && item.TenantId == session.TenantId && item.StartsAtUtc < dayEndUtc &&
            item.Status != AppointmentStatus.Cancelled && item.Status != AppointmentStatus.Completed && item.Status != AppointmentStatus.NoShow &&
            (!assignedUserId.HasValue || item.AssignedUserId == assignedUserId)).ToListAsync(cancellationToken);
        var blocks = await _context.AppointmentBlocks.AsNoTracking().Where(item =>
            item.CompanyId == session.CompanyId && item.TenantId == session.TenantId && item.IsActive &&
            item.StartsAtUtc < dayEndUtc && item.EndsAtUtc > dayStartUtc &&
            (!assignedUserId.HasValue || item.AssignedUserId == null || item.AssignedUserId == assignedUserId)).ToListAsync(cancellationToken);

        for (var slot = dayStartUtc; slot.AddMinutes(duration) <= dayEndUtc; slot = slot.AddMinutes(company.AppointmentSlotIntervalMinutes))
        {
            var end = slot.AddMinutes(duration);
            if (appointments.Any(item => item.StartsAtUtc < end && item.EndsAtUtc > slot)) continue;
            if (blocks.Any(item => item.StartsAtUtc < end && item.EndsAtUtc > slot)) continue;
            if (slot < DateTime.UtcNow) continue;
            result.Slots.Add(new AppointmentAvailabilitySlotDto { StartsAtUtc = slot, EndsAtUtc = end });
        }
        return result;
    }

    public async Task<IReadOnlyList<AppointmentBlockDto>> GetBlocksAsync(WorkspaceSessionContext session, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session); ValidateUtcRange(fromUtc, toUtc);
        return await _context.AppointmentBlocks.AsNoTracking().Where(item => item.CompanyId == session.CompanyId && item.TenantId == session.TenantId && item.IsActive && item.StartsAtUtc < toUtc && item.EndsAtUtc > fromUtc)
            .OrderBy(item => item.StartsAtUtc).Select(item => new AppointmentBlockDto { Id = item.Id, AssignedUserId = item.AssignedUserId, StartsAtUtc = item.StartsAtUtc, EndsAtUtc = item.EndsAtUtc, Reason = item.Reason }).ToListAsync(cancellationToken);
    }

    public async Task<AppointmentBlockDto> CreateBlockAsync(WorkspaceSessionContext session, CreateAppointmentBlockRequestDto request, CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session); await ValidateAssignedUserAsync(session, request.AssignedUserId, cancellationToken);
        var block = new AppointmentBlock(session.TenantId, session.CompanyId, request.StartsAtUtc, request.EndsAtUtc, request.AssignedUserId, request.Reason);
        await _context.AppointmentBlocks.AddAsync(block, cancellationToken); await _context.SaveChangesAsync(cancellationToken);
        return new AppointmentBlockDto { Id = block.Id, AssignedUserId = block.AssignedUserId, StartsAtUtc = block.StartsAtUtc, EndsAtUtc = block.EndsAtUtc, Reason = block.Reason };
    }

    public async Task DeleteBlockAsync(WorkspaceSessionContext session, Guid blockId, CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        var block = await _context.AppointmentBlocks.FirstOrDefaultAsync(item => item.Id == blockId && item.CompanyId == session.CompanyId && item.TenantId == session.TenantId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Bloqueio nao encontrado.");
        block.Deactivate(); await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AppointmentReportDto> GetReportAsync(WorkspaceSessionContext session, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session); ValidateUtcRange(fromUtc, toUtc);
        var rows = await _context.Appointments.AsNoTracking().Where(item => item.CompanyId == session.CompanyId && item.TenantId == session.TenantId && item.StartsAtUtc >= fromUtc && item.StartsAtUtc < toUtc).Select(item => new { item.Status, item.CustomerOrderId, item.UnitPriceSnapshot }).ToListAsync(cancellationToken);
        return new AppointmentReportDto
        {
            Total = rows.Count, Requested = rows.Count(x => x.Status == AppointmentStatus.Requested), Confirmed = rows.Count(x => x.Status == AppointmentStatus.Confirmed),
            InProgress = rows.Count(x => x.Status == AppointmentStatus.InProgress), Completed = rows.Count(x => x.Status == AppointmentStatus.Completed),
            Cancelled = rows.Count(x => x.Status == AppointmentStatus.Cancelled), NoShow = rows.Count(x => x.Status == AppointmentStatus.NoShow),
            LinkedRevenue = rows.Where(x => x.CustomerOrderId.HasValue && x.Status == AppointmentStatus.Completed).Sum(x => x.UnitPriceSnapshot)
        };
    }

    public async Task<IReadOnlyList<AppointmentProfessionalDto>> GetProfessionalsAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        return await _context.Users.AsNoTracking().Where(item => item.CompanyId == session.CompanyId && item.TenantId == session.TenantId && item.IsActive)
            .OrderBy(item => item.FullName).Select(item => new AppointmentProfessionalDto { Id = item.Id, Name = item.FullName, Role = item.Role.ToString() }).ToListAsync(cancellationToken);
    }

    private IQueryable<Appointment> BaseQuery(bool tracking)
    {
        var query = _context.Appointments
            .Include(item => item.Pet)
            .Include(item => item.AssignedUser)
            .AsQueryable();
        return tracking ? query : query.AsNoTracking();
    }

    private async Task<Appointment> GetEntityAsync(
        WorkspaceSessionContext session,
        Guid appointmentId,
        bool tracking,
        CancellationToken cancellationToken)
    {
        return await BaseQuery(tracking).FirstOrDefaultAsync(item =>
            item.Id == appointmentId &&
            item.TenantId == session.TenantId &&
            item.CompanyId == session.CompanyId,
            cancellationToken) ?? throw new KeyNotFoundException("Agendamento nao encontrado.");
    }

    private async Task ValidateAssignedUserAsync(WorkspaceSessionContext session, Guid? userId, CancellationToken cancellationToken)
    {
        if (!userId.HasValue) return;

        var exists = await _context.Users.AsNoTracking().AnyAsync(item =>
            item.Id == userId.Value &&
            item.TenantId == session.TenantId &&
            item.CompanyId == session.CompanyId &&
            item.IsActive,
            cancellationToken);
        if (!exists) throw new KeyNotFoundException("Profissional nao encontrado.");
    }

    private async Task EnsureNoConflictAsync(
        WorkspaceSessionContext session,
        Guid? ignoredAppointmentId,
        Guid petId,
        Guid? assignedUserId,
        DateTime startsAtUtc,
        int durationMinutes,
        CancellationToken cancellationToken)
    {
        if (startsAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("O horario deve estar em UTC.", nameof(startsAtUtc));
        if (durationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "A duracao deve ser positiva.");

        var endsAtUtc = startsAtUtc.AddMinutes(durationMinutes);
        var blocked = await _context.AppointmentBlocks.AsNoTracking().AnyAsync(item =>
            item.CompanyId == session.CompanyId && item.TenantId == session.TenantId && item.IsActive &&
            item.StartsAtUtc < endsAtUtc && item.EndsAtUtc > startsAtUtc &&
            (item.AssignedUserId == null || item.AssignedUserId == assignedUserId), cancellationToken);
        if (blocked) throw new InvalidOperationException("O horario esta bloqueado para agendamentos.");
        var candidates = await _context.Appointments.AsNoTracking()
            .Where(item =>
                item.TenantId == session.TenantId &&
                item.CompanyId == session.CompanyId &&
                item.Id != ignoredAppointmentId &&
                item.IsActive &&
                item.Status != AppointmentStatus.Cancelled &&
                item.Status != AppointmentStatus.Completed &&
                item.Status != AppointmentStatus.NoShow &&
                item.StartsAtUtc < endsAtUtc &&
                (item.PetId == petId || (assignedUserId.HasValue && item.AssignedUserId == assignedUserId)))
            .ToListAsync(cancellationToken);

        if (candidates.Any(item => item.EndsAtUtc > startsAtUtc))
            throw new InvalidOperationException("O horario conflita com outro agendamento do animal ou profissional.");
    }

    private static void EnsurePetShop(WorkspaceSessionContext session)
    {
        BusinessCapabilityGuard.Require(session.Capabilities.HasAppointments, "Appointments");
    }

    private static void ValidateUtcRange(DateTime fromUtc, DateTime toUtc)
    {
        if (fromUtc.Kind != DateTimeKind.Utc || toUtc.Kind != DateTimeKind.Utc || toUtc <= fromUtc)
            throw new ArgumentException("Informe um intervalo UTC valido.");
        if (toUtc - fromUtc > TimeSpan.FromDays(93))
            throw new ArgumentException("O intervalo de consulta nao pode ultrapassar 93 dias.");
    }

    private Task<Company> GetCompanyAsync(WorkspaceSessionContext session, bool tracking, CancellationToken cancellationToken)
    {
        var query = _context.Companies.AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return query.FirstAsync(item => item.Id == session.CompanyId && item.TenantId == session.TenantId && item.IsActive, cancellationToken);
    }

    private async Task AddHistoryAsync(WorkspaceSessionContext session, Appointment appointment, AppointmentStatus previous, AppointmentStatus next, string? reason, CancellationToken cancellationToken)
    {
        await _context.AppointmentStatusHistories.AddAsync(new AppointmentStatusHistory(
            session.TenantId, session.CompanyId, appointment.Id, session.UserId, previous, next, DateTime.UtcNow, reason), cancellationToken);
    }

    private static AppointmentScheduleSettingsDto MapSettings(Company company) => new()
    {
        ServiceDays = company.AppointmentServiceDays,
        StartTime = company.AppointmentStartTime.ToString("HH:mm"),
        EndTime = company.AppointmentEndTime.ToString("HH:mm"),
        SlotIntervalMinutes = company.AppointmentSlotIntervalMinutes,
        TimeZone = company.TimeZoneId
    };

    private static AppointmentDto MapToDto(Appointment item) => new()
    {
        Id = item.Id,
        PetId = item.PetId,
        PetName = item.Pet.Name,
        MenuItemId = item.MenuItemId,
        ServiceName = item.ServiceNameSnapshot,
        CustomerOrderId = item.CustomerOrderId,
        AssignedUserId = item.AssignedUserId,
        AssignedUserName = item.AssignedUser?.FullName,
        StartsAtUtc = item.StartsAtUtc,
        EndsAtUtc = item.EndsAtUtc,
        DurationMinutes = item.DurationMinutes,
        Status = item.Status,
        UnitPrice = item.UnitPriceSnapshot,
        CustomerNotes = item.CustomerNotes,
        InternalNotes = item.InternalNotes,
        CancellationReason = item.CancellationReason
    };
}
