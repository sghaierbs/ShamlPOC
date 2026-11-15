
using Elsa.Extensions;
using Shaml.Application.Abstractions.Workflows;
using Shaml.Infrastructure;
using Shaml.Infrastructure.Persistence.Messaging;
using Shaml.Worker.Integration;
using Shaml.Workflows;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// 1. Shaml Business Infrastructure
// =====================================================
builder.Services.AddInfrastructure(builder.Configuration);

// =====================================================
// 2. Elsa Runtime
// =====================================================
builder.Services.AddShamlWorkflows(builder.Configuration);

// =====================================================
// 3. Workflow Startup Integration
// =====================================================
builder.Services.AddScoped<ICaseWorkflowStarter, ElsaCaseWorkflowStarter>();

// =====================================================
// 4. Transactional Outbox
// =====================================================
builder.Services.AddScoped<IOutboxMessagePublisher, CaseCreatedOutboxPublisher>();

builder.Services.AddScoped<OutboxDispatcher>();

// =====================================================
// 5. Background Processing
// =====================================================
builder.Services.AddHostedService<OutboxBackgroundService>();

// =====================================================
// 6. ASP.NET Core Services
// =====================================================
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

var app = builder.Build();

// =====================================================
// 7. HTTP Middleware
// =====================================================
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// =====================================================
// 8. Endpoints
// =====================================================
app.MapHealthChecks("/health");
app.MapWorkflowsApi();

// =====================================================
// 9. Start Worker
// =====================================================
await app.RunAsync();
