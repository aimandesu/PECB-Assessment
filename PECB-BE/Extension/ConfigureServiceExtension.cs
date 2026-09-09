using Microsoft.EntityFrameworkCore;
using PECB_BE.Database;
using PECB_BE.Infrastructure;
using PECB_BE.Repository;

namespace PECB_BE.Extension;

public static class ConfigureServiceExtension
{
    public static void ConfigureService(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddDbContext<ApplicationDbContext>(options => 
            options.UseSqlServer(connectionString));
        
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter()
                );
        });


        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IAgentRepository, AgentRepository>();

    }
}