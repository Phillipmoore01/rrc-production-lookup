using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Rrc.Production.Data;

public static class ServiceCollectionExtensions
{
    public const string ConnectionStringName = "Production";

    public static IServiceCollection AddProductionDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        return services.AddDbContext<ProductionDbContext>(options => options.UseSqlServer(connectionString));
    }
}
