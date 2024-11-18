using System.Text.Json;
using Aegis.Server.Data;
using Aegis.Server.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Profiling.SqlFormatters;
using StackExchange.Profiling.Storage;
using StackExchange.Profiling;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Microsoft.AspNetCore.Diagnostics;
using Volo.Abp.Localization;
using Volo.Abp.VirtualFileSystem;
using Sample.License.Web.Data.Context;
using Sample.License.Web.DTOs;
using Sample.License.Web.Filters;
using Sample.License.Web.Localization;

namespace Sample.License.Web;

[DependsOn(typeof(AbpAspNetCoreMvcModule),
    typeof(AbpAutofacModule),
    typeof(AbpLocalizationModule))]
public class SampleLicenseServerMvcModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .CreateLogger();
        context.Services.AddControllers();
        context.Services.AddDbContext<AegisDbContext, ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        context.Services.AddMvc(options => { options.Filters.Add<ApiExceptionFilter>(); });
        context.Services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        context.Services.AddAegisServer();
        context.Services.AddMemoryCache();
        context.Services.AddSerilog();
        context.Services.AddEndpointsApiExplorer();
        context.Services.AddSwaggerGen();
        context.Services.AddMiniProfiler(options =>
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

        // Register custom services here
        var containerBuilder = context.Services.GetContainerBuilder();

        // Configure ABP Services
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<SampleLicenseServerMvcModule>("Aegis.Server.AspNetCore");
        });
        Configure<AbpLocalizationOptions>(options =>
        {
            options.DefaultResourceType = typeof(FeaturesResource);
            options.Resources
                .Add<FeaturesResource>("en")
                .AddVirtualJson("/Localization/Resources/Features");
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        if (env.IsDevelopment())
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
                appError.Run(async httpContext =>
                {
                    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    httpContext.Response.ContentType = "application/json";

                    var contextFeature = httpContext.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        var error = new { message = contextFeature.Error.Message };
                        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(error));

                        // Logging
                        Log.Error(contextFeature.Error, "An unhandled exception occurred.");
                        Console.WriteLine($"Stack Trace: {contextFeature.Error.StackTrace}");
                    }
                });
            });
    }
}