using System.Net;
using Carter;
using FluentValidation;
using PECB_BE.Infrastructure;
using PECB_BE.Shared;
using PECB_BE.Shared.Response;

namespace PECB_BE.Features.Agent.Get;

public class GetAgent
{
    public record Request(string AgentId);
    
    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.AgentId)
                .NotEmpty()
                .WithMessage("Agent ID is required.")
                .Must(BeAValidGuid).WithMessage("Agent ID must be a valid GUID layout.");
        }
        
        private bool BeAValidGuid(string agentId)
        {
            return Guid.TryParse(agentId, out _);
        }
        
        public class Endpoint : ICarterModule
        {
            public void AddRoutes(IEndpointRouteBuilder app)
            {
                app.MapGet("/agent/get", GetAgent);
            }

            private async Task<ResultResponse<Entities.Agent?>> GetAgent(
                [AsParameters] Request r,
                IValidator<Request> validator,
                IAgentRepository  agentRepository,
                CancellationToken cancellationToken)
            {
                var validationResult = await validator.ValidateAsync(
                    r, cancellationToken);
            
                if (!validationResult.IsValid)
                {
                    return ResultResponse<Entities.Agent?>.Failure(
                        new Error(
                            HttpStatusCode.BadRequest,
                            "GetAgent.Validation",
                            validationResult.ToString())
                    );
                }
                
                var agent = await agentRepository.GetAgent(Guid.Parse(r.AgentId));
                
                if (agent == null)
                {
                    return ResultResponse<Entities.Agent?>
                        .Success(
                            agent,
                            description: "Agent not found.");
                }
                
                return ResultResponse<Entities.Agent?>.Success(agent);
                
            }
        }
    }
    
}