using DDDworkshop.Dam.NoDdd.Api.Data;
using DDDworkshop.Dam.NoDdd.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ⚠️ ANTI-PATTERN: Everything registered in one flat list — no layering.
// Compare to the DDD API where registrations are grouped by layer
// (domain, application, infrastructure) with clear responsibilities.

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Data store — singleton so data survives across requests (same as DDD repos)
builder.Services.AddSingleton<InMemoryDataStore>();

// ⚠️ Services registered directly — no interfaces, no abstractions.
// Hard to mock for testing, hard to swap implementations.
builder.Services.AddScoped<RightsService>();
builder.Services.AddScoped<LicenseService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "NoDDD Rights API v1");
    });
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
