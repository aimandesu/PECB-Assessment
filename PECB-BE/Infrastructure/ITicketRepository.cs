using PECB_BE.Entities;
using PECB_BE.Enums;
using PECB_BE.Features.Ticket.GetAll;
using PECB_BE.Shared;

namespace PECB_BE.Infrastructure;

public interface ITicketRepository
{
    Task<Ticket> Create(Ticket ticket);
    Task<Ticket?> GetTicket(Guid ticketId);
    IQueryable<Ticket> GetTickets(GetAllTicket.Request r);
    Task<Ticket?> Update(Ticket ticket);
    Task<Ticket?> Delete(Guid ticketId);
    Task<Ticket?> AssignAgent(Guid ticketId, Agent agent);
    Task<Ticket?> UnassignAgent(Guid ticketId, Agent agent);
    Task<Ticket?> UpdateStatus(Guid ticketId, Status status);
}