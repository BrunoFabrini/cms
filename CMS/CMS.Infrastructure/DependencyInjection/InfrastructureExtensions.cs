using CMS.Application.Services;
using CMS.Domain.Repositories.Events;
using CMS.Infrastructure.Persistence;
using CMS.Infrastructure.Persistence.Repositories.Events;
using CMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Infrastructure.DependencyInjection;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddInfrastructureServices();
        services.AddApplicationServices();

        services.AddDatabase();
        services.AddRepositories();

        return services;
    }

    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
    }

    public static void AddInfrastructureServices(this IServiceCollection services)
    {
        //services.AddScoped<ILogService, LogService>();
        //TODO - AddLogger
    }

    public static void AddDatabase(this IServiceCollection services)
    {
        services.AddDbContext<CmsDbContext>((provider, options) =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("CMS");
            options.UseSqlServer(connectionString);
        });
    }

    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IEventReadRepository, EventReadRepository>();
        services.AddScoped<IEventWriteRepository, EventWriteRepository>();
    }
}
