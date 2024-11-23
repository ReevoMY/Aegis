using ByteDash.Manpower.LicenseServer.Data;
using ByteDash.Manpower.LicenseServer.Swagger;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.AntiForgery;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.SqlServer;
using Volo.Abp.Modularity;
using Volo.Abp.Swashbuckle;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.VirtualFileSystem;

namespace ByteDash.Manpower.LicenseServer;

[DependsOn(
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpAutofacModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpCachingModule),
    typeof(AbpSwashbuckleModule),
    typeof(AbpAspNetCoreSerilogModule),

    // Tenant Management module packages
    //typeof(AbpTenantManagementHttpApiModule),
    //typeof(AbpTenantManagementApplicationModule),

    // Entity Framework Core packages for the used modules
    //typeof(AbpTenantManagementEntityFrameworkCoreModule),
    typeof(AbpEntityFrameworkCoreSqlServerModule)
)]
public class ManpowerLicenseServerModule : AbpModule
{
    /* Single point to enable/disable multi-tenancy */
    private const bool IsMultiTenant = false;

    #region ConfigureServices

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        var configuration = context.Services.GetConfiguration();

        ConfigureWebApp(context.Services);
        //ConfigureMultiTenancy();
        ConfigureUrls(configuration);
        ConfigureAutoMapper(context);
        ConfigureSwagger(context.Services);
        ConfigureVirtualFiles(hostingEnvironment);
        ConfigureEfCore(context);

        Configure<AbpAntiForgeryOptions>(options =>
        {
            //options.TokenCookie.Expiration = TimeSpan.FromDays(365);
            //options.AutoValidateIgnoredHttpMethods.Remove("GET");
            options.AutoValidate = false;
        });
    }

    private void ConfigureWebApp(IServiceCollection services)
    {
        services.AddControllers();
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
        });
    }

    //private void ConfigureMultiTenancy()
    //{
    //    Configure<AbpMultiTenancyOptions>(options =>
    //    {
    //        options.IsEnabled = IsMultiTenant;
    //    });
    //}

    private void ConfigureUrls(IConfiguration configuration)
    {
        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
        });
    }

    private void ConfigureVirtualFiles(IWebHostEnvironment hostingEnvironment)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<ManpowerLicenseServerModule>();
            if (hostingEnvironment.IsDevelopment())
            {
                /* Using physical files in development, so we don't need to recompile on changes */
                options.FileSets.ReplaceEmbeddedByPhysical<ManpowerLicenseServerModule>(hostingEnvironment.ContentRootPath);
            }
        });
    }

    private void ConfigureSwagger(IServiceCollection services)
    {
        services.AddAbpSwaggerGen(
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "LicenseServer API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
                options.HideAbpEndpoints();
                //options.AddSecurityDefinition("X-CSRF-TOKEN", new OpenApiSecurityScheme
                //{
                //    In = ParameterLocation.Header,
                //    Name = "X-CSRF-TOKEN",
                //    Type = SecuritySchemeType.ApiKey,
                //    Description = "CSRF Token"
                //});
                options.OperationFilter<AddAntiForgeryTokenHeaderParameter>();
            }
        );
    }

    private void ConfigureAutoMapper(ServiceConfigurationContext context)
    {
        context.Services.AddAutoMapperObjectMapper<ManpowerLicenseServerModule>();
        Configure<AbpAutoMapperOptions>(options =>
        {
            /* Uncomment `validate: true` if you want to enable the Configuration Validation feature.
             * See AutoMapper's documentation to learn what it is:
             * https://docs.automapper.org/en/stable/Configuration-validation.html
             */
            options.AddMaps<ManpowerLicenseServerModule>(/* validate: true */);
        });
    }

    private void ConfigureEfCore(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<ManpowerLicenseServerDbContext>(options =>
        {
            /* You can remove "includeAllEntities: true" to create
             * default repositories only for aggregate roots
             * Documentation: https://docs.abp.io/en/abp/latest/Entity-Framework-Core#add-default-repositories
             */
            options.AddDefaultRepositories(includeAllEntities: true);
        });

        Configure<AbpDbContextOptions>(options =>
        {
            options.Configure(configurationContext =>
            {
                configurationContext.UseSqlServer();
            });
        });

    }

    #endregion

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = (context.GetApplicationBuilder() as WebApplication)!;
        var env = context.GetEnvironment();

        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseAbpSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Manpower License Server API");
                options.DocExpansion(DocExpansion.None);
                options.DisplayRequestDuration();
                options.EnableDeepLinking();
                options.EnableFilter();
            });
        }

        app.UseCorrelationId();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
    }
}