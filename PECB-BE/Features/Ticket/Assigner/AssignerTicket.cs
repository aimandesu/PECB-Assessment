using System.Net;
using Carter;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using PECB_BE.Enums;
using PECB_BE.Infrastructure;
using PECB_BE.Shared;
using PECB_BE.Shared.Dto;
using PECB_BE.Shared.Response;

namespace PECB_BE.Features.Ticket.Assigner;

public class AssignerTicket
{
    public record Request(
        string TicketId, 
        string AgentId,
        AssignerOption AssignerOption);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId)
                .NotEmpty()
                .WithMessage("Ticket ID is required.")
                .Must(BeAValidGuid)
                .WithMessage("Ticket ID must be a valid GUID layout.");
            
            RuleFor(x => x.AgentId)
                .NotEmpty()
                .WithMessage("Agent ID is required.")
                .Must(BeAValidGuid)
                .WithMessage("Agent ID must be a valid GUID layout.");
            
            RuleFor(x => x.AssignerOption)
                .IsInEnum()
                .WithMessage("Option is invalid.");
            
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
            app.MapPatch("/ticket/assigner", AssignerTicket);
        }

        private async Task<ResultResponse<TicketDto?>> AssignerTicket(
            Request r,
            IValidator<Request> validator,
            IAgentRepository agentRepository,
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
                        "AssignTicket.Validation",
                        validationResult.ToString())
                );
            }

            var agent = await agentRepository.GetAgent(Guid.Parse(r.AgentId));

            if (agent == null)
            {
                return ResultResponse<TicketDto?>
                    .Success(
                        null,
                        HttpStatusCode.NotFound,
                        "Agent not found.");
            }

            var updatedTicket = await (r.AssignerOption switch
            {
                AssignerOption.Assign => ticketRepository.AssignAgent(
                    Guid.Parse(r.TicketId), 
                    agent),
        
                AssignerOption.Unassign => ticketRepository.UnassignAgent(
                    Guid.Parse(r.TicketId),
                    agent),
        
                _ => throw new ArgumentOutOfRangeException()
            });
            
            var dto = updatedTicket.Adapt<TicketDto>();

            if (updatedTicket == null)
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