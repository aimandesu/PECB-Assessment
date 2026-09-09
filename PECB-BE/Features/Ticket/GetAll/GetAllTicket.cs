using Carter;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PECB_BE.Database;
using PECB_BE.Enums;
using PECB_BE.Infrastructure;
using PECB_BE.Shared;
using PECB_BE.Shared.Dto;

namespace PECB_BE.Features.Ticket.GetAll;

public static class GetAllTicket
{
    public record Request(
        Search? Search,
        Filter? Filter);

    public record Search(
        string Reference,
        string Title,
        string CustomerName);

    public record Filter(
        Status? Status,
        Priority? Priority,
        Guid? AgentId,
        bool IsTicketOverdue);
    
    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/ticket/get-all", GetAllTicket);
        }

        private async Task<Paginate<TicketDto>> GetAllTicket(
            [FromBody] Request r,
            ITicketRepository ticketRepository,
            CancellationToken cancellationToken,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query =  ticketRepository.GetTickets(r);

            var result = await Paginate<Entities.Ticket>.Pagination(
                query,
                pageNumber,
                pageSize,
                cancellationToken);
            
            var mappedData = result.Data
                .Select((t) => t.Adapt<TicketDto>())
                .ToList();

            return result.TransformTo(mappedData);

        }
    }
}