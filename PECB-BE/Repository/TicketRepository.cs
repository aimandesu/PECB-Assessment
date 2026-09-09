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
        
        if (r.Search is not null)
        {
            if (string.IsNullOrEmpty(r.Search.Title))
            {
                query = query.Where(t => 
                    t.Title == r.Search.Title);
            }

            if (string.IsNullOrEmpty(r.Search.CustomerName))
            {
                query = query.Where(t => 
                    t.CustomerName == r.Search.CustomerName);
            }

            if (string.IsNullOrEmpty(r.Search.Reference))
            {
                query = query.Where(t => 
                    t.Reference == r.Search.Reference);
            }
                
        }

        if (r.Filter is not null)
        {
            if (r.Filter.Status.HasValue)
            {
                query = query.Where(t => 
                    t.Status == r.Filter.Status.Value);
            }

            if (r.Filter.Priority.HasValue)
            {
                query = query.Where(t => 
                    t.Priority == r.Filter.Priority.Value);
            }

            if (r.Filter.AgentId.HasValue)
            {
                query = query.Where(t => 
                    t.AssignedAgent == r.Filter.AgentId.Value);
            }
                
            query = query.Where(t => 
                t.IsOverdue == r.Filter.IsTicketOverdue);
                
        }
        
        return query;
        
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