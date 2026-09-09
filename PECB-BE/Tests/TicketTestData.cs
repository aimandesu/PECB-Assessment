using PECB_BE.Entities;
using PECB_BE.Enums;

namespace PECB_BE.Tests;

/// <summary>
/// Builders for the domain entities under test.
/// </summary>
internal static class TicketTestData
{
    /// <summary>
    /// Builds a ticket in the requested state.
    /// </summary>
    /// <remarks>
    /// <c>Priority</c> is assigned before <c>Status</c> on purpose. The <c>Priority</c>
    /// setter calls <c>RecalculateDueDate</c>, which returns early when the status is
    /// already Resolved or Closed, so the reverse order would leave <c>DueDate</c> unset.
    /// Both production call sites (CreateTicket, UpdateTicket) use this same order.
    /// </remarks>
    public static Ticket NewTicket(
        Priority priority = Priority.Normal,
        Status status = Status.New) =>
        new()
        {
            Title = "Cannot log in",
            Description = "The portal rejects valid credentials.",
            CustomerName = "Acme Ltd",
            CustomerEmail = "ops@acme.test",
            Priority = priority,
            Status = status,
        };

    public static Agent NewAgent(bool active = true) =>
        new()
        {
            FullName = "Dana Reyes",
            Department = Department.Technical,
            Active = active,
        };

    /// <summary>A ticket already moved into In progress through the legal route.</summary>
    public static Ticket InProgressTicket(out Agent agent)
    {
        var ticket = NewTicket();
        agent = NewAgent();
        ticket.AssignAgent(agent);
        ticket.UpdateStatus(Status.InProgress);
        return ticket;
    }
}
