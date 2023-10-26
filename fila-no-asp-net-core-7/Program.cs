using fila_no_asp_net_core_7.Interfaces;
using fila_no_asp_net_core_7.Models;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using Serilog.Events;
using System.Threading.RateLimiting;


Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("Logs/app.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);

    //adding rate limiting
    builder.Services.AddRateLimiter(_ => _.AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 15000;
        options.Window = TimeSpan.FromSeconds(60);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 60000;
    }));

    //serilog (before all middleware)
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddMemoryCache();

    builder.Services.AddSingleton<IHub<SaveRequest, SaveResponse>, SaveHub>();
    builder.Services.AddSingleton<IHub<ProcessRequest, ProcessResponse>, ProcessHub>();

    var app = builder.Build();

    app.UseRateLimiter();

    //serilog (before all handlers)
    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline.
    // Swagger even in production
    app.UseSwagger();
    app.UseSwaggerUI();

    //app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application didn't start correctly");
}
finally
{
    Log.CloseAndFlush();
}


