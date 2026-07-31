using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using Xunit;

namespace ZeroPaper.Tests.Domain;

public sealed class AppointmentTests
{
    [Fact]
    public void Complete_requires_an_in_progress_appointment()
    {
        var appointment = CreateAppointment();

        Assert.Throws<InvalidOperationException>(() => appointment.Complete(DateTime.UtcNow));
    }

    [Fact]
    public void Terminal_appointment_cannot_be_rescheduled()
    {
        var appointment = CreateAppointment();
        appointment.Cancel("Cliente solicitou", DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => appointment.Reschedule(DateTime.UtcNow.AddDays(2), 60));
    }

    [Fact]
    public void Valid_lifecycle_reaches_completed()
    {
        var appointment = CreateAppointment();
        var confirmedAt = DateTime.UtcNow;
        appointment.Confirm(confirmedAt);
        appointment.Start(confirmedAt.AddMinutes(1));
        appointment.Complete(confirmedAt.AddMinutes(30));

        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
        Assert.NotNull(appointment.CompletedAtUtc);
    }

    private static Appointment CreateAppointment() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        DateTime.UtcNow.AddDays(1),
        60,
        "Banho",
        80m);
}
