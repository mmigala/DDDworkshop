using DDDworkshop.Dam.Rights.Api.Middleware;
using DDDworkshop.Dam.Rights.Application.Abstractions;
using DDDworkshop.Dam.Rights.Application.Handlers;
using DDDworkshop.Dam.Rights.Domain.Events;
using DDDworkshop.Dam.Rights.Domain.Policies;
using DDDworkshop.Dam.Rights.Domain.Repositories;
using DDDworkshop.Dam.Rights.Infrastructure.EventHandlers;
using DDDworkshop.Dam.Rights.Infrastructure.Repositories;
using DDDworkshop.Dam.Rights.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// DI Registration
// ---------------------------------------------------------------------------

// ASP.NET Core MVC controllers
builder.Services.AddControllers();

// OpenAPI / Swagger
builder.Services.AddOpenApi();

// --- Domain layer ---
// Policy (pure domain service – depends only on domain interfaces)
builder.Services.AddScoped<IExclusiveLicensingPolicy, ExclusiveLicensingPolicy>();

// --- Infrastructure layer ---
// In-memory repositories (singleton so data survives across requests)
builder.Services.AddSingleton<IAssetRightsRepository, InMemoryAssetRightsRepository>();
builder.Services.AddSingleton<ILicenseGrantRepository, InMemoryLicenseGrantRepository>();

// Clock
builder.Services.AddSingleton<IClock, SystemClock>();

// Domain event dispatcher (uses IServiceProvider to resolve handlers)
builder.Services.AddScoped<IDomainEventDispatcher, InProcessDomainEventDispatcher>();

// Domain event handlers
builder.Services.AddScoped<IDomainEventHandler<LicenseGrantedEvent>, LicenseGrantedEventHandler>();
builder.Services.AddScoped<IDomainEventHandler<LicenseRevokedEvent>, LicenseRevokedEventHandler>();

// --- Application layer ---
// Command handlers
builder.Services.AddScoped<RequestLicenseHandler>();
builder.Services.AddScoped<RevokeLicenseHandler>();
builder.Services.AddScoped<SetRightsProfileHandler>();
builder.Services.AddScoped<AddRestrictionHandler>();
builder.Services.AddScoped<AddExclusiveWindowHandler>();

// Query handlers
builder.Services.AddScoped<QueryHandlers>();

// ---------------------------------------------------------------------------
// Build & Configure Pipeline
// ---------------------------------------------------------------------------

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Swagger UI available at /swagger
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "DDDworkshop Rights API v1");
    });
}

app.UseHttpsRedirection();

// Domain exception → HTTP status code mapping
app.UseMiddleware<DomainExceptionMiddleware>();

app.MapControllers();

app.Run();
