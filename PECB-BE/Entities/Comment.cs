using System.ComponentModel.DataAnnotations;

namespace PECB_BE.Entities;

public class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required Guid TicketId { get; set; }
    [MaxLength(100)]
    public required string AuthorName { get; set; }
    [MaxLength(200)]
    public required string Body { get; set; }
    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
    
    //navigational property
    public Ticket? Ticket { get; set; }
    
}