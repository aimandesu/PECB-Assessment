using System.ComponentModel.DataAnnotations;
using PECB_BE.Enums;

namespace PECB_BE.Entities;

public class Agent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(100)]
    public required string FullName { get; set; }

    public required Department Department { get; set; }
    public bool Active { get; set; } = true;
    
    //navigation property
    public List<Ticket> Tickets { get; set; } = [];
}