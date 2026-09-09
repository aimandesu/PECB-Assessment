using System.Net;
using Microsoft.EntityFrameworkCore;
using PECB_BE.Database;
using PECB_BE.Entities;
using PECB_BE.Enums;
using PECB_BE.Features.Ticket.GetAll;
using PECB_BE.Infrastructure;
using PECB_BE.Shared;
using PECB_BE.Shared.Response;

namespace PECB_BE.Repository;

public class TicketRepository(
    ApplicationDbContext context,
    CancellationToken cancellationToken = default) : ITicketRepository
{
    public async Task<Ticket> Create(Ticket ticket)
    {
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync(cancellationToken);
        return ticket;
    }

    public async Task<Ticket?> GetTicket(Guid ticketId)
    {
        var ticket = await context.Tickets
            .Include(t => t.Comments)
            .AsNoTracking()
            // .Include(t => t.Agent)
            .FirstOrDefaultAsync(t => t.Id == ticketId);
        
        return ticket;
    }

    public IQueryable<Ticket> GetTickets(GetAllTicket.Request r)
    {
        var query = context.Tickets
            .AsNoTracking()
            .AsQueryable();

        // One search box, matched against the three fields a user would search by.
        // The terms are OR'd: requiring all three to match would never return a row.
        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            var term = r.Search.Trim();

            query = query.Where(t =>
                t.Reference.Contains(term) ||
                t.Title.Contains(term) ||
                t.CustomerName.Contains(term));
        }

        if (r.Status.HasValue)
        {
            query = query.Where(t => t.Status == r.Status.Value);
        }

        if (r.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == r.Priority.Value);
        }

        if (r.AgentId.HasValue)
        {
            query = query.Where(t => t.AssignedAgent == r.AgentId.Value);
        }

        // Ticket.IsOverdue is a computed CLR property that EF cannot translate, so the
        // same rule is expressed here in terms EF can turn into SQL. The filter is only
        // applied when the caller actually asked for it.
        if (r.IsTicketOverdue.HasValue)
        {
            var now = DateTimeOffset.UtcNow;

            query = r.IsTicketOverdue.Value
                ? query.Where(t =>
                    t.DueDate < now &&
                    t.Status != Status.Resolved &&
                    t.Status != Status.Closed)
                : query.Where(t =>
                    t.DueDate >= now ||
                    t.Status == Status.Resolved ||
                    t.Status == Status.Closed);
        }

        // SQL Server needs a deterministic ordering for OFFSET/FETCH paging; without one
        // EF falls back to an arbitrary order and rows can repeat or vanish across pages.
        // TicketNumber is the identity column behind Reference, so this is newest-first.
        return query.OrderByDescending(t => EF.Property<int>(t, "TicketNumber"));
    }

    public async Task<Ticket?> Update(Ticket ticket)
    {
        var searchTicket = await GetTicket(ticket.Id);

        if (searchTicket is null)
        {
            return null;
        }

        searchTicket.UpdateStatus(ticket.Status);
        
        searchTicket.Title = ticket.Title;
        searchTicket.Description = ticket.Description;
        searchTicket.CustomerName = ticket.CustomerName;
        searchTicket.CustomerEmail = ticket.CustomerEmail;
        searchTicket.Priority = ticket.Priority;
        searchTicket.LastModifiedDate = ticket.LastModifiedDate;
        
        context.Tickets.Update(searchTicket);
        await context.SaveChangesAsync(cancellationToken);

        return searchTicket;
    }

    public async Task<Ticket?> Delete(Guid ticketId)
    {
        var searchTicket = await GetTicket(ticketId);
        
        if (searchTicket is null)
        {
            return null;
        }
        
        context.Tickets.Remove(searchTicket);
        await context.SaveChangesAsync(cancellationToken);
        return searchTicket;
    }

    public async Task<Ticket?> AssignAgent(Guid ticketId, Agent agent)
    {
        var searchTicket = await GetTicket(ticketId);

        if (searchTicket is null)
        {
            return null;
        }

        searchTicket.AssignAgent(agent);
        
        context.Tickets.Update(searchTicket);
        await context.SaveChangesAsync(cancellationToken);
        
        return searchTicket;

    }

    public async Task<Ticket?> UnassignAgent(Guid ticketId, Agent agent)
    {
        var searchTicket = await GetTicket(ticketId);

        if (searchTicket is null)
        {
            return null;
        }
        
        searchTicket.UnassignAgent(agent);
        
        context.Tickets.Update(searchTicket);
        await context.SaveChangesAsync(cancellationToken);
        
        return searchTicket;
    }

    public async Task<Ticket?> UpdateStatus(Guid ticketId, Status status)
    {
        var searchTicket = await GetTicket(ticketId);

        if (searchTicket is null)
        {
            return null;
        }
        
        searchTicket.UpdateStatus(status);
        
        context.Tickets.Update(searchTicket);
        await context.SaveChangesAsync(cancellationToken);

        return searchTicket;

    }
}