using Microsoft.EntityFrameworkCore;

namespace PECB_BE.Database;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    
    public DbSet<Entities.Ticket> Tickets { get; set; }
    public DbSet<Entities.Agent> Agents { get; set; }
    public DbSet<Entities.Comment> Comments { get; set; }
    
    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}