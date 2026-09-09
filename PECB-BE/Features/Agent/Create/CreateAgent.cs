using System.Net;
using Carter;
using FluentValidation;
using PECB_BE.Enums;
using PECB_BE.Infrastructure;
using PECB_BE.Shared;
using PECB_BE.Shared.Response;

namespace PECB_BE.Features.Agent.Create;

public class CreateAgent
{
    public record Request(
        string FullName,
        Department Department);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(request => request.FullName)
                .NotEmpty()
                .WithMessage("Please specify a FullName");
            
            RuleFor(request => request.Department)
                .IsInEnum()
                .WithMessage("Please specify a Department");
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/agent/create", CreateAgent);
        }

        private async Task<ResultResponse<Entities.Agent>> CreateAgent(
            Request r,
            IValidator<Request> validator,
            IAgentRepository agentRepository,
            CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(
                r, cancellationToken);
            
            if (!validationResult.IsValid)
            {
                return ResultResponse<Entities.Agent>.Failure(
                    new Error(
                        HttpStatusCode.BadRequest,
                        "CreateAgent.Validation",
                        validationResult.ToString())
                );
            }

            var agent = new Entities.Agent
            {
                FullName = r.FullName,
                Department = r.Department
            };
            
            var createdAgent = await agentRepository.CreateAgent(agent);
            
            return ResultResponse<Entities.Agent>
                .Success(
                    createdAgent,
                    description: "The agent was created successfully");

        }
    }
    
}