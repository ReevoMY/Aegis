using System.Text.Json;
using Aegis.Server.AspNetCore.Data.Context;
using Aegis.Server.AspNetCore.DTOs;
using Aegis.Server.AspNetCore.Filters;
using Aegis.Server.AspNetCore.Services;
using Aegis.Server.Data;
using Aegis.Server.Extensions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Profiling;
using StackExchange.Profiling.SqlFormatters;
using StackExchange.Profiling.Storage;

var builder = WebApplication.CreateBuilder(args);

// Configure Logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

// Configure Services
builder.Services.AddControllers();
builder.Services.AddDbContext<AegisDbContext, ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddMvc(options => { options.Filters.Add<ApiExceptionFilter>(); });
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddScoped<AuthService>();
builder.Services.AddAegisServer();
builder.Services.AddMemoryCache();
builder.Services.AddSerilog();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMiniProfiler(options =>
{
    options.RouteBasePath = "/profiler";
    (options.Storage as MemoryCacheStorage)!.CacheDuration = TimeSpan.FromMinutes(60);
    options.SqlFormatter = new VerboseSqlServerFormatter();
    options.TrackConnectionOpenClose = true;
    options.ColorScheme = ColorScheme.Auto;
    options.PopupDecimalPlaces = 2;
    options.EnableMvcFilterProfiling = true;
    options.EnableMvcViewProfiling = true;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection()
    .UseMiniProfiler()
    .UseSerilogRequestLogging()
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization()
    .UseEndpoints(endpoints => { endpoints.MapControllers(); })
    .UseExceptionHandler(appError =>
    {
        appError.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
            if (contextFeature != null)
            {
                var error = new { message = contextFeature.Error.Message };
                await context.Response.WriteAsync(JsonSerializer.Serialize(error));

                // Logging
                Log.Error(contextFeature.Error, "An unhandled exception occurred.");
                Console.WriteLine($"Stack Trace: {contextFeature.Error.StackTrace}");
            }
        });
    });

app.Run();
