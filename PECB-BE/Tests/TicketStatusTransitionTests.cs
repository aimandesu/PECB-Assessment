using PECB_BE.Enums;
using Xunit;
using static PECB_BE.Tests.TicketTestData;

namespace PECB_BE.Tests;

/// <summary>
/// Covers the workflow enforced by <c>Ticket.UpdateStatus</c>:
/// New -> In progress (requires an assigned agent) -> Resolved -> Closed,
/// with Resolved able to reopen into In progress, and Closed terminal.
/// </summary>
public class TicketStatusTransitionTests
{
    [Fact]
    public void New_to_InProgress_is_rejected_while_no_agent_is_assigned()
    {
        var ticket = NewTicket();

        var error = Assert.Throws<InvalidOperationException>(
            () => ticket.UpdateStatus(Status.InProgress));

        Assert.Contains("Invalid status transition", error.Message);
        Assert.Equal(Status.New, ticket.Status);
    }

    [Fact]
    public void New_to_InProgress_succeeds_once_an_agent_is_assigned()
    {
        var ticket = NewTicket();
        ticket.AssignAgent(NewAgent());

        ticket.UpdateStatus(Status.InProgress);

        Assert.Equal(Status.InProgress, ticket.Status);
        Assert.Null(ticket.ResolvedDate);
        Assert.Null(ticket.ClosedDate);
    }

    [Fact]
    public void InProgress_to_Resolved_stamps_the_resolved_date()
    {
        var ticket = InProgressTicket(out _);
        var before = DateTimeOffset.UtcNow;

        ticket.UpdateStatus(Status.Resolved);

        Assert.Equal(Status.Resolved, ticket.Status);
        Assert.NotNull(ticket.ResolvedDate);
        Assert.InRange(ticket.ResolvedDate!.Value, before, DateTimeOffset.UtcNow);
        Assert.Null(ticket.ClosedDate);
    }

    [Fact]
    public void Resolved_to_Closed_stamps_the_closed_date_and_seals_the_ticket()
    {
        var ticket = InProgressTicket(out _);
        ticket.UpdateStatus(Status.Resolved);
        var before = DateTimeOffset.UtcNow;

        ticket.UpdateStatus(Status.Closed);

        Assert.Equal(Status.Closed, ticket.Status);
        Assert.NotNull(ticket.ClosedDate);
        Assert.InRange(ticket.ClosedDate!.Value, before, DateTimeOffset.UtcNow);
        Assert.True(ticket.IsReadOnly);
    }

    [Fact]
    public void Resolved_can_be_reopened_into_InProgress()
    {
        var ticket = InProgressTicket(out _);
        ticket.UpdateStatus(Status.Resolved);

        ticket.UpdateStatus(Status.InProgress);

        Assert.Equal(Status.InProgress, ticket.Status);
        Assert.False(ticket.IsReadOnly);
    }

    [Theory]
    // A ticket may not skip a step in the workflow.
    [InlineData(Status.New, Status.Resolved)]
    [InlineData(Status.New, Status.Closed)]
    [InlineData(Status.InProgress, Status.Closed)]
    // Nor move backwards outside the one supported reopen path.
    [InlineData(Status.InProgress, Status.New)]
    [InlineData(Status.Resolved, Status.New)]
    public void Transitions_outside_the_workflow_are_rejected(Status from, Status to)
    {
        var ticket = NewTicket(status: from);
        ticket.AssignAgent(NewAgent());

        var error = Assert.Throws<InvalidOperationException>(() => ticket.UpdateStatus(to));

        Assert.Contains("Invalid status transition", error.Message);
        Assert.Equal(from, ticket.Status);
    }

    [Theory]
    [InlineData(Status.New)]
    [InlineData(Status.InProgress)]
    [InlineData(Status.Resolved)]
    public void A_closed_ticket_rejects_every_further_transition(Status target)
    {
        var ticket = NewTicket(status: Status.Closed);

        var error = Assert.Throws<InvalidOperationException>(() => ticket.UpdateStatus(target));

        Assert.Contains("closed and read-only", error.Message);
        Assert.Equal(Status.Closed, ticket.Status);
    }

    [Fact]
    public void Re_applying_the_current_status_is_a_no_op_rather_than_an_error()
    {
        var ticket = InProgressTicket(out _);

        ticket.UpdateStatus(Status.InProgress);

        Assert.Equal(Status.InProgress, ticket.Status);
        Assert.Null(ticket.ResolvedDate);
    }

    [Fact]
    public void Re_applying_Closed_to_a_closed_ticket_does_not_throw()
    {
        // The equality check runs before the read-only guard, so this is allowed.
        // UpdateTicket relies on it when saving field edits without a status change.
        var ticket = NewTicket(status: Status.Closed);

        ticket.UpdateStatus(Status.Closed);

        Assert.Equal(Status.Closed, ticket.Status);
    }
}
