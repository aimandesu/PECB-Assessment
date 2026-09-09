using System.Net;
using Carter;
using FluentValidation;
using Mapster;
using PECB_BE.Enums;
// using PECB_BE.Features.Ticket.Create;
using PECB_BE.Infrastructure;
using PECB_BE.Shared;
using PECB_BE.Shared.Dto;
using PECB_BE.Shared.Response;

namespace PECB_BE.Features.Ticket.Update;

public static class UpdateTicket
{
    public record Request(
        Guid Id,
        string Title,
        string Description,
        string CustomerName,
        string CustomerEmail,
        Priority Priority,
        Status Status);
    
    public class Validator : AbstractValidator<UpdateTicket.Request>
    {
        public Validator()
        {
            RuleFor(request => request.Title)
                .NotEmpty()
                .WithMessage("Please specify a Title");
            
            RuleFor(request => request.Description)
                .NotEmpty()
                .WithMessage("Please specify a Description");
            
            RuleFor(request => request.CustomerName)
                .NotEmpty()
                .WithMessage("Please specify a CustomerName");
            
            RuleFor(request => request.CustomerEmail)
                .NotEmpty()
                .WithMessage("Please specify a CustomerEmail");
            
            RuleFor(request => request.Priority)
                .IsInEnum()
                .WithMessage("Please specify a Priority");
            
            RuleFor(request => request.Status)
                .IsInEnum()
                .WithMessage("Please specify a Status");
            
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPatch("/ticket/update", UpdateTicket);
        }

        private async Task<ResultResponse<TicketDto?>> UpdateTicket(
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
                        "UpdateTicket.Validation",
                        validationResult.ToString())
                );
            }

            var ticket = new Entities.Ticket
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                CustomerName = r.CustomerName,
                CustomerEmail = r.CustomerEmail,
                Priority = r.Priority,
                Status = r.Status,
            };
            
            var updatedTicket = await ticketRepository
                .Update(ticket);

            if (updatedTicket == null)
            {
                return ResultResponse<TicketDto?>
                    .Success(
                        null,
                        description: "Ticket not found");
            }
            
            var dto = updatedTicket.Adapt<TicketDto>();
            
            return ResultResponse<TicketDto?>
                .Success(dto);

        }
    }
    
}