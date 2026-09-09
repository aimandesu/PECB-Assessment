using Microsoft.EntityFrameworkCore;
using PECB_BE.Database;
using PECB_BE.Entities;
using PECB_BE.Enums;
using PECB_BE.Infrastructure;

namespace PECB_BE.Repository;

public class AgentRepository(
    ApplicationDbContext context,
    CancellationToken cancellationToken = default) : IAgentRepository
{
    public async Task<Agent?> GetAgent(Guid id)
    {
        var agent = await context.Agents
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);
        
        return agent;
    }

    public IQueryable<Agent> GetAllAgents(Department? department)
    {
        var query =  context.Agents
            .AsNoTracking()
            .AsQueryable();

        if (department.HasValue)
        {
            query = query.Where((q) => q.Department == department.Value);
        }
        
        return query;
        
    }

    public async Task<Agent> CreateAgent(Agent agent)
    {
        context.Agents.Add(agent);
        await context.SaveChangesAsync(cancellationToken);
        
        return agent;
        
    }
}