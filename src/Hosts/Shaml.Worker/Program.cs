using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shaml.Infrastructure;
using Shaml.Infrastructure.Persistence.Messaging;
using Shaml.Workflows;

var builder = WebApplication.CreateBuilder(args);

// Business database and infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// Elsa runtime, persistence, identity, and authentication
builder.Services.AddShamlWorkflows(builder.Configuration);

// Temporarily disable dispatcher until Elsa publisher is implemented.
builder.Services.RemoveAll<OutboxDispatcher>();

// Transactional outbox dispatcher
// builder.Services.AddHostedService<OutboxBackgroundService>();

// Authorization services
builder.Services.AddAuthorization();

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// HTTP middleware
app.UseRouting();

// Elsa Identity authentication
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint
app.MapHealthChecks("/health");

// Elsa workflow management and runtime APIs
app.MapWorkflowsApi();

await app.RunAsync();