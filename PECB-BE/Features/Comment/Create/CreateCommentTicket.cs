using System.Net;
using Carter;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PECB_BE.Database;
using PECB_BE.Infrastructure;
using PECB_BE.Shared;
using PECB_BE.Shared.Response;

namespace PECB_BE.Features.Comment.Create;

public class CreateCommentTicket
{
    public record Request(
        string TicketId,
        string AuthorName,
        string Body);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId)
                .NotEmpty()
                .WithMessage("Ticket ID is required.");
            
            RuleFor(x => x.AuthorName)
                .NotEmpty()
                .WithMessage("Author name is required.");
            
            RuleFor(x => x.Body)
                .NotEmpty()
                .WithMessage("Body is required.");
            
        }
    }
    
    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/comment/ticket/create", CreateCommentTicket);
        }

        private async Task<ResultResponse<Entities.Comment>> CreateCommentTicket(
            [FromBody] Request r,
            IValidator<Request> validator,
            ITicketRepository ticketRepository,
            ApplicationDbContext context,
            CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(
                r, cancellationToken);
            
            if (!validationResult.IsValid)
            {
                return ResultResponse<Entities.Comment>.Failure(
                    new Error(
                        HttpStatusCode.BadRequest,
                        "Comment.Validation",
                        validationResult.ToString())
                );
            }
            
            var ticket = await ticketRepository.GetTicket(Guid.Parse(r.TicketId));

            if (ticket == null)
            {
                return ResultResponse<Entities.Comment>
                    .Failure(
                        new Error(
                            HttpStatusCode.NotFound,
                            "Ticket Not Found")
                        );
            }
            
            var comment = new Entities.Comment
            {
                TicketId = Guid.Parse(r.TicketId),
                AuthorName = r.AuthorName,
                Body = r.Body,
            };
            
            var commentChecker = ticket.AddComment(comment);
            
            context.Comments.Add(commentChecker);
            await context.SaveChangesAsync(cancellationToken);
            
            return ResultResponse<Entities.Comment>.Success(comment);
            
        }
    }
}