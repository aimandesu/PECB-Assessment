using System.Net;
using Carter;
using FluentValidation;
using Mapster;
using PECB_BE.Database;
using PECB_BE.Enums;
using PECB_BE.Infrastructure;
using PECB_BE.Shared;
using PECB_BE.Shared.Dto;
using PECB_BE.Shared.Response;

namespace PECB_BE.Features.Ticket.Create;

public static class CreateTicket
{
    public record Request(
            string Title,
            string Description,
            string CustomerName,
            string CustomerEmail,
            Priority Priority);

    public class Validator : AbstractValidator<Request>
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
        }
    }
    
    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/ticket/create", CreateTicket);
        }

        private async Task<ResultResponse<TicketDto>> CreateTicket(
            Request r,
            IValidator<Request> validator,
            ITicketRepository ticketRepository,
            CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(
                r, cancellationToken);
            
            if (!validationResult.IsValid)
            {
                return ResultResponse<TicketDto>.Failure(
                    new Error(
                        HttpStatusCode.BadRequest,
                        "CreateTicket.Validation",
                        validationResult.ToString())
                );
            }

            var ticket = new Entities.Ticket
            {
                Title = r.Title,
                Description = r.Description,
                CustomerName = r.CustomerName,
                CustomerEmail = r.CustomerEmail,
                Priority = r.Priority,
                Status = Status.New
            };
            
            var createdTicket = await ticketRepository.Create(ticket);

            var dto = createdTicket.Adapt<TicketDto>();
            
            return ResultResponse<TicketDto>
                .Success(
                    dto,
                    description: "The ticket was created successfully");

        }
    }
}