using Aegis.Server.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseAutofac();
await builder.Services.AddApplicationAsync<SampleLicenseServerMvcModule>();

var app = builder.Build();
await app.InitializeApplicationAsync();
await app.RunAsync();
