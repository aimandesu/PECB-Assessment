using PECB_BE.Enums;
using Xunit;
using static PECB_BE.Tests.TicketTestData;

namespace PECB_BE.Tests;

/// <summary>
/// Covers the SLA the system derives from a ticket's priority:
/// Critical 4 hours, High 1 day, Normal 3 days, Low 7 days from creation.
/// </summary>
public class TicketDueDateTests
{
    [Theory]
    [InlineData(Priority.Critical, 4)]
    [InlineData(Priority.High, 24)]
    [InlineData(Priority.Normal, 72)]
    [InlineData(Priority.Low, 168)]
    public void Due_date_is_derived_from_the_priority(Priority priority, int expectedHours)
    {
        // The ticket stamps its own creation time, so the expected due date is bracketed
        // by the clock readings either side of construction.
        var before = DateTimeOffset.UtcNow;
        var ticket = NewTicket(priority);
        var after = DateTimeOffset.UtcNow;

        Assert.InRange(
            ticket.DueDate,
            before.AddHours(expectedHours),
            after.AddHours(expectedHours));
    }

    [Fact]
    public void Raising_the_priority_brings_the_due_date_forward()
    {
        var ticket = NewTicket(Priority.Low);
        var lowDueDate = ticket.DueDate;

        ticket.Priority = Priority.Critical;

        Assert.True(
            ticket.DueDate < lowDueDate,
            "Escalating Low to Critical must shorten the deadline.");
        Assert.InRange(
            ticket.DueDate,
            DateTimeOffset.UtcNow.AddHours(4).AddSeconds(-30),
            DateTimeOffset.UtcNow.AddHours(4).AddSeconds(30));
    }

    [Fact]
    public void Lowering_the_priority_pushes_the_due_date_out()
    {
        var ticket = NewTicket(Priority.Critical);
        var criticalDueDate = ticket.DueDate;

        ticket.Priority = Priority.Normal;

        Assert.True(ticket.DueDate > criticalDueDate);
    }

    [Fact]
    public void The_due_date_is_measured_from_creation_not_from_the_priority_change()
    {
        // Both tickets are created at the same moment, so changing one to Critical must
        // land on the same deadline as one created as Critical outright.
        var escalated = NewTicket(Priority.Low);
        var born = NewTicket(Priority.Critical);

        escalated.Priority = Priority.Critical;

        Assert.True(
            (escalated.DueDate - born.DueDate).Duration() < TimeSpan.FromSeconds(5),
            "A re-prioritised ticket must not have its clock restarted.");
    }

    [Fact]
    public void A_resolved_ticket_keeps_its_deadline_when_re_prioritised()
    {
        var ticket = InProgressTicket(out _);
        ticket.UpdateStatus(Status.Resolved);
        var dueDateWhenResolved = ticket.DueDate;

        ticket.Priority = Priority.Critical;

        Assert.Equal(dueDateWhenResolved, ticket.DueDate);
    }

    [Fact]
    public void A_freshly_created_ticket_is_not_overdue()
    {
        var ticket = NewTicket(Priority.Critical);

        Assert.False(ticket.IsOverdue);
    }

    [Theory]
    [InlineData(Status.Resolved)]
    [InlineData(Status.Closed)]
    public void A_finished_ticket_is_never_reported_as_overdue(Status status)
    {
        var ticket = NewTicket(Priority.Critical, status);

        Assert.False(ticket.IsOverdue);
    }
}
