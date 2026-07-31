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

    public AppointmentService(ZeroPaperDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AppointmentDto>> GetAsync(
        WorkspaceSessionContext session,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        ValidateUtcRange(fromUtc, toUtc);

        var appointments = await BaseQuery(tracking: false)
            .Where(item =>
                item.TenantId == session.TenantId &&
                item.CompanyId == session.CompanyId &&
                item.StartsAtUtc >= fromUtc &&
                item.StartsAtUtc < toUtc)
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
        await EnsureNoConflictAsync(session, appointment.Id, appointment.PetId, appointment.AssignedUserId, request.StartsAtUtc, request.DurationMinutes, cancellationToken);
        appointment.Reschedule(request.StartsAtUtc, request.DurationMinutes);
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

    public async Task<AppointmentDto> ChangeStatusAsync(
        WorkspaceSessionContext session,
        Guid appointmentId,
        ChangeAppointmentStatusRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        var appointment = await GetEntityAsync(session, appointmentId, tracking: true, cancellationToken);
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

        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(session, appointment.Id, cancellationToken);
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
        if (session.BusinessSegment != BusinessSegment.PetShop)
            throw new UnauthorizedAccessException("O modulo de agenda nao esta disponivel para esta empresa.");
    }

    private static void ValidateUtcRange(DateTime fromUtc, DateTime toUtc)
    {
        if (fromUtc.Kind != DateTimeKind.Utc || toUtc.Kind != DateTimeKind.Utc || toUtc <= fromUtc)
            throw new ArgumentException("Informe um intervalo UTC valido.");
        if (toUtc - fromUtc > TimeSpan.FromDays(93))
            throw new ArgumentException("O intervalo de consulta nao pode ultrapassar 93 dias.");
    }

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
