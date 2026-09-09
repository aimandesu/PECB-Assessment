using Mapster;
using PECB_BE.Entities;
using PECB_BE.Shared.Dto;

namespace PECB_BE.Shared.Mapper;

public class TicketMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Ticket, TicketDto>()
            // Convert Enums to user-friendly strings instead of numbers
            .Map(
                dest => dest.Priority, 
                src => src.Priority.ToString())
            .Map(
                dest => dest.Status, 
                src => src.Status.ToString())
            
            // Map the private CreatedDate / AssignedAgent properties if they are exposed
            .Map(
                dest => dest.AssignedAgent,
                src => src.AssignedAgent)
            .Map(
                dest => dest.IsOverdue, 
                src => src.IsOverdue)
            
            // Mapster automatically handles matching lists (Ticket.Comments -> TicketDto.Comments) 
            // if property names match or if you explicitly map them:
            .Map(
                dest => dest.Comments,
                src => src.Comments);
    }
}