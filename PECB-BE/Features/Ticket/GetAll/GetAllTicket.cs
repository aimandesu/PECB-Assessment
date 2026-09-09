using Carter;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using PECB_BE.Enums;
using PECB_BE.Infrastructure;
using PECB_BE.Shared;
using PECB_BE.Shared.Dto;

namespace PECB_BE.Features.Ticket.GetAll;

public static class GetAllTicket
{
    /// <summary>
    /// Search and filter criteria for the ticket list.
    /// </summary>
    /// <remarks>
    /// These arrive on the query string, not in a request body: a browser cannot
    /// attach a body to a GET request, so criteria bound with [FromBody] would never
    /// reach this endpoint from the client.
    /// </remarks>
    public record Request(
        string? Search = null,
        Status? Status = null,
        Priority? Priority = null,
        Guid? AgentId = null,
        bool? IsTicketOverdue = null);

    /// <summary>Upper bound on page size so a client cannot ask for the whole table.</summary>
    private const int MaxPageSize = 100;

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/ticket/get-all", GetAllTicket);
        }

        private async Task<Paginate<TicketDto>> GetAllTicket(
            ITicketRepository ticketRepository,
            CancellationToken cancellationToken,
            [FromQuery] string? search = null,
            [FromQuery] Status? status = null,
            [FromQuery] Priority? priority = null,
            [FromQuery] Guid? agentId = null,
            [FromQuery] bool? isTicketOverdue = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var request = new Request(search, status, priority, agentId, isTicketOverdue);

            var query = ticketRepository.GetTickets(request);

            var result = await Paginate<Entities.Ticket>.Pagination(
                query,
                pageNumber < 1 ? 1 : pageNumber,
                Math.Clamp(pageSize, 1, MaxPageSize),
                cancellationToken);

            var mappedData = result.Data
                .Select(t => t.Adapt<TicketDto>())
                .ToList();

            return result.TransformTo(mappedData);
        }
    }
}
