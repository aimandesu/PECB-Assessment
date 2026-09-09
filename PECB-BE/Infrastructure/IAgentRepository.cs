using PECB_BE.Entities;
using PECB_BE.Enums;

namespace PECB_BE.Infrastructure;

public interface IAgentRepository
{
    Task<Agent?> GetAgent(Guid id);
    IQueryable<Agent> GetAllAgents(Department? department);
    Task<Agent> CreateAgent(Agent agent);
}