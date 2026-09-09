using PECB_BE.Entities;
using PECB_BE.Enums;
using Xunit;
using static PECB_BE.Tests.TicketTestData;

namespace PECB_BE.Tests;

/// <summary>
/// Covers agent assignment and the read-only guard on comments.
/// </summary>
public class TicketAssignmentAndCommentTests
{
    [Fact]
    public void Assigning_an_active_agent_records_the_assignee()
    {
        var ticket = NewTicket();
        var agent = NewAgent();

        ticket.AssignAgent(agent);

        Assert.Equal(agent.Id, ticket.AssignedAgent);
    }

    [Fact]
    public void An_inactive_agent_cannot_be_assigned()
    {
        var ticket = NewTicket();
        var agent = NewAgent(active: false);

        var error = Assert.Throws<InvalidOperationException>(() => ticket.AssignAgent(agent));

        Assert.Contains("inactive agent", error.Message);
        Assert.Null(ticket.AssignedAgent);
    }

    [Fact]
    public void Unassigning_clears_the_assignee()
    {
        var ticket = NewTicket();
        var agent = NewAgent();
        ticket.AssignAgent(agent);

        ticket.UnassignAgent(agent);

        Assert.Null(ticket.AssignedAgent);
    }

    [Fact]
    public void An_agent_cannot_unassign_a_ticket_that_belongs_to_someone_else()
    {
        var ticket = NewTicket();
        var assignee = NewAgent();
        var stranger = NewAgent();
        ticket.AssignAgent(assignee);

        Assert.Throws<InvalidOperationException>(() => ticket.UnassignAgent(stranger));
        Assert.Equal(assignee.Id, ticket.AssignedAgent);
    }

    [Fact]
    public void Comments_are_accepted_while_the_ticket_is_open()
    {
        var ticket = NewTicket();
        var comment = NewComment(ticket.Id);

        var accepted = ticket.AddComment(comment);

        Assert.Same(comment, accepted);
    }

    [Fact]
    public void Comments_are_rejected_once_the_ticket_is_closed()
    {
        var ticket = NewTicket(status: Status.Closed);

        var error = Assert.Throws<InvalidOperationException>(
            () => ticket.AddComment(NewComment(ticket.Id)));

        Assert.Contains("closed and read-only", error.Message);
    }

    private static Comment NewComment(Guid ticketId) =>
        new()
        {
            TicketId = ticketId,
            AuthorName = "Dana Reyes",
            Body = "Asked the customer to retry after a password reset.",
        };
}
