using CMS.Application.Services;
using CMS.Domain.Repositories.Content;
using CMS.Domain.Repositories.Events;
using CMS.Infrastructure.Options;
using CMS.Infrastructure.Persistence;
using CMS.Infrastructure.Persistence.Repositories.Content;
using CMS.Infrastructure.Persistence.Repositories.Events;
using CMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CMS.Infrastructure.DependencyInjection;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddAppAuthentication();
        services.AddOptions(configuration);
        services.AddInfrastructureServices();
        services.AddApplicationServices();

        services.AddDatabase();
        services.AddRepositories();

        return services;
    }

    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IContentManagementService, ContentManagementService>();
        services.AddScoped<IContentQueryService, ContentQueryService>();
        services.AddScoped<IContentAdministrationService, ContentAdministrationService>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddHostedService<EventConsumerBackgroundService>();
    }

    public static void AddOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EventConsumerOptions>(configuration.GetSection(EventConsumerOptions.SectionName));
        services.Configure<EventIngestionOptions>(configuration.GetSection(EventIngestionOptions.SectionName));
    }

    public static void AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSerilog((_, loggerConfiguration) => loggerConfiguration
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console());

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
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
        services.AddScoped<IContentRepository, ContentRepository>();
        services.AddScoped<IContentReadRepository, ContentReadRepository>();
    }
}
