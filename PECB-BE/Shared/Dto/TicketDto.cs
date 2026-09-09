namespace PECB_BE.Shared.Dto;

public record TicketDto(
    Guid Id,
    string Reference,
    string Title,
    string Description,
    
    // Flattened Customer Info (or keep separate if the UI needs it)
    string CustomerName,
    string CustomerEmail,
    
    // Enums converted to strings or left as integers depending on frontend needs
    string Priority, 
    string Status,
    
    Guid? AssignedAgent,
    
    // Dates formatted clearly for the client
    DateTimeOffset CreatedDate,
    DateTimeOffset LastModifiedDate,
    DateTimeOffset DueDate,
    DateTimeOffset? ResolvedDate,
    DateTimeOffset? ClosedDate,
    
    // Computed domain property exposed directly to the UI
    bool IsOverdue,
    
    // Nested collection using a dedicated, simplified Comment DTO
    List<TicketCommentDto> Comments
);

// A simple nested DTO to prevent exposing the full Comment database entity
public record TicketCommentDto(
    Guid Id,
    string AuthorName,
    string Body,
    DateTimeOffset CreatedDate
);
