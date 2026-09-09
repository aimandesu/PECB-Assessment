using System.Net;
using Carter;
using FluentValidation;
using Mapster;
using PECB_BE.Enums;
using PECB_BE.Infrastructure;
using PECB_BE.Shared;
using PECB_BE.Shared.Dto;
using PECB_BE.Shared.Response;

namespace PECB_BE.Features.Ticket.Update;

public class UpdateStatus
{
    public record Request(string TicketId, Status Status);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId)
                .NotEmpty()
                .WithMessage("Ticket ID is required.")
                .Must(BeAValidGuid)
                .WithMessage("Ticket ID must be a valid GUID layout.");
            
            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Status is invalid.");
            
        }
        
        private bool BeAValidGuid(string id)
        {
            return Guid.TryParse(id, out _);
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPatch("/ticket/status", UpdateStatus);
        }

        private async Task<ResultResponse<TicketDto?>> UpdateStatus(
            Request r,
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
                        "UpdateStatus.Validation",
                        validationResult.ToString())
                );
            }

            var updatedTicketStatus = await ticketRepository.UpdateStatus(
                Guid.Parse(r.TicketId),
                r.Status);
            
            var dto = updatedTicketStatus.Adapt<TicketDto>();
            
            if (updatedTicketStatus == null)
            {
                return ResultResponse<TicketDto?>
                    .Success(
                        null,
                        HttpStatusCode.NotFound,
                        "Ticket not found.");
            }
            
            return ResultResponse<TicketDto?>
                .Success(dto);

        }
    }
    
}