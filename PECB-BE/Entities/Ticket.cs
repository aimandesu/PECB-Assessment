using System.ComponentModel.DataAnnotations;
using PECB_BE.Enums;

namespace PECB_BE.Entities;

public class Ticket
{
    private Priority _priority;
    private Status _status;
    
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(100)]
    public string Reference { get; set; } = string.Empty;
    [MaxLength(100)]
    public required string Title { get; set; }
    [MaxLength(250)]
    public required string Description { get; set; }
    [MaxLength(50)]
    public required string CustomerName { get; set; }
    [MaxLength(250)]
    public required string CustomerEmail { get; set; }

    public required Priority Priority 
    { 
        get => _priority;
        set 
        {
            _priority = value;
            RecalculateDueDate();
        }
    }
    
    public required Status Status 
    { 
        get => _status; 
        init => _status = value;
        // set
        // {
        //     _status = value;
        //     UpdateStatus(value);
        // }
    }
    public Guid? AssignedAgent { get; private set; }
    private DateTimeOffset CreatedDate { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastModifiedDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedDate { get; set; }
    public DateTimeOffset? ClosedDate { get; set; }
    //DueDate calculated by the system
    public DateTimeOffset DueDate { get; private set; }
    
    //navigation property
    public List<Comment> Comments { get; set; } = [];
    // public Agent? Agent { get; set; }
    
    //functionality
    
    public bool IsReadOnly => Status == Status.Closed;
    
    private void RecalculateDueDate()
    {
        if (Status is Status.Closed or Status.Resolved)
        {
            return;
        }
        
        DueDate = Priority switch
        {
            Priority.Critical => CreatedDate.AddHours(4),
            Priority.High => CreatedDate.AddDays(1),
            Priority.Normal => CreatedDate.AddDays(3),
            Priority.Low => CreatedDate.AddDays(7),
            _ => CreatedDate.AddDays(3)
        };
        
    }

    public void UpdateStatus(Status newStatus)
    {
        if (Status == newStatus) return;
        
        if (IsReadOnly)
        {
            throw new InvalidOperationException(
                "Cannot change status: This ticket is closed and read-only.");
        }
        
        var isValid = (Status, newStatus) switch
        {
            (Status.New, Status.InProgress) => AssignedAgent is not null,
            (Status.InProgress, Status.Resolved) => true,
            (Status.Resolved, Status.Closed) => true,
            (Status.Resolved, Status.InProgress) => true,
            _ => false
        };

        if (!isValid)
        {
            throw new InvalidOperationException(
                $"Invalid status transition from {Status} to {newStatus}." +
                $"Either because agent not assigned yet or status not following flow");
        }
        
        _status = newStatus;

        if (Status == Status.Resolved)
        {
            ResolvedDate = DateTimeOffset.UtcNow;
        }

        if (Status == Status.Closed)
        {
            ClosedDate = DateTimeOffset.UtcNow;
        }
        
    }

    public void AssignAgent(Agent agent)
    {
        if (!agent.Active)
        {
            throw new InvalidOperationException(
                $"Cannot assign inactive agent: {agent.Id} to a ticket.");
        }
        
        AssignedAgent = agent.Id;
    }

    public void UnassignAgent(Agent agent)
    {
        if (AssignedAgent != agent.Id)
        {
            throw new InvalidOperationException(
                $"Cannot unassign inactive agent: {agent.Id} to a ticket that doesnt belong to this agent.");
        }
        
        AssignedAgent = null;
    }
    
    public Comment AddComment(Comment comment)
    {
        if (IsReadOnly)
        {
            throw new InvalidOperationException(
                "Cannot change status: This ticket is closed and read-only.");
        }
        
        // Comments.Add(comment);
        return comment;
    }
    
    public bool IsOverdue => DateTimeOffset.UtcNow > DueDate 
                             && Status is not Status.Resolved
                             && Status is not Status.Closed;
    
}