using System.Net;
using Carter;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PECB_BE.Enums;
using PECB_BE.Infrastructure;
using PECB_BE.Shared;
using PECB_BE.Shared.Response;

namespace PECB_BE.Features.Agent.GetAll;

public class GetAllAgent
{
    public record Request(
        Department? Department);

    public class Validator : AbstractValidator<Request>
    {
        public  Validator()
        {
            RuleFor(x => x.Department)
                .IsInEnum()
                .WithMessage("Option is invalid.");
        }
    }
    
    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/agent/get-all", GetAllAgent);
        }

        private async Task<ResultResponse<List<Entities.Agent>>> GetAllAgent(
            [AsParameters] Request r,
            IValidator<Request> validator,
            IAgentRepository agentRepository,
            CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(
                r, cancellationToken);
            
            if (!validationResult.IsValid)
            {
                return ResultResponse<List<Entities.Agent>>.Failure(
                    new Error(
                        HttpStatusCode.BadRequest,
                        "GetAllAgent.Validation",
                        validationResult.ToString())
                );
            }
            
            var query = agentRepository
                .GetAllAgents(r.Department);

            var result = await query.ToListAsync(cancellationToken);
            
            return ResultResponse<List<Entities.Agent>>
                .Success(result);

        }
    }
}