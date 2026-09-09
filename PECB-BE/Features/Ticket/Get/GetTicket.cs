using System.Net;
using Carter;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using PECB_BE.Infrastructure;
using PECB_BE.Shared;
using PECB_BE.Shared.Dto;
using PECB_BE.Shared.Response;

namespace PECB_BE.Features.Ticket.Get;

public static class GetTicket
{
    public record Request(string TicketId);
    
    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId)
                .NotEmpty().WithMessage("Ticket ID is required.")
                .Must(BeAValidGuid).WithMessage("Ticket ID must be a valid GUID layout.");
        }

        private bool BeAValidGuid(string ticketId)
        {
            return Guid.TryParse(ticketId, out _);
        }
    }
    
    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/ticket/get", GetTicket);
        }

        private async Task<ResultResponse<TicketDto?>> GetTicket(
            [AsParameters] Request r,
            IValidator<Request> validator,
            ITicketRepository ticketRepository,
            CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(
                r, cancellationToken);
            
            if (!validationResult.IsValid)
            {
                return ResultResponse<TicketDto?>.Failure(
                    new Error(
                        HttpStatusCode.BadRequest,
                        "GetTicket.Validation",
                        validationResult.ToString())
                );
            }
            
            var ticket = await ticketRepository.GetTicket(
                Guid.Parse(r.TicketId));
            
            var dto = ticket.Adapt<TicketDto>();

            if (ticket == null)
            {
                return ResultResponse<TicketDto?>
                    .Success(
                        dto,
                        description: "Ticket not found.");
            }
            
            return ResultResponse<TicketDto?>
                .Success(dto);
            
        }
    }
}