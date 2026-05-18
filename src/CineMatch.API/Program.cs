using CineMatch.API.Middleware;
using CineMatch.Application;
using CineMatch.Infrastructure;
using Scalar.AspNetCore;

namespace CineMatch.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });

        // Each layer owns its registrations — see Infrastructure.DependencyInjection,
        // Application.DependencyInjection and API.DependencyInjection.
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddApplication();
        builder.Services.AddApiServices();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        // Pipeline order matters: exception handler first (so it catches everything below),
        // then auth before authorization. Reordering will silently break error responses or [Authorize] checks.
        // UseCors must come before UseAuthentication — preflight OPTIONS requests must be handled first.
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseHttpsRedirection();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
