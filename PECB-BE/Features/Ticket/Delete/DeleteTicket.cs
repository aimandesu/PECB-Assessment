using System.Net;
using Carter;
using FluentValidation;
using Mapster;
using PECB_BE.Infrastructure;
using PECB_BE.Shared;
using PECB_BE.Shared.Dto;
using PECB_BE.Shared.Response;

namespace PECB_BE.Features.Ticket.Delete;

public static class DeleteTicket
{
    public record Request(string TicketId);
    
    public class Validator : AbstractValidator<DeleteTicket.Request>
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
            app.MapDelete("/ticket/delete", DeleteTicket);
        }

        private async Task<ResultResponse<TicketDto?>> DeleteTicket(
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
                        "DeleteTicket.Validation",
                        validationResult.ToString())
                );
            }
            
            var deletedTicket = await ticketRepository.Delete(
                Guid.Parse(r.TicketId));
            
            var dto = deletedTicket.Adapt<TicketDto>();
            
            if (deletedTicket == null)
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