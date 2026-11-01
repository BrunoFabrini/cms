using CMS.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure();

var app = builder.Build();

await app.Services.ApplyMigrationsAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
