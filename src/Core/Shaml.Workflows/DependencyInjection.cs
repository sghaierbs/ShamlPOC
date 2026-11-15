
using Elsa.Extensions;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Identity;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shaml.Workflows.Definitions;

namespace Shaml.Workflows;

public static class DependencyInjection
{
    public static IServiceCollection AddShamlWorkflows(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("ElsaDatabase")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:ElsaDatabase is required.");

        var adminUserName =
            configuration["Identity:Bootstrap:UserName"]
            ?? throw new InvalidOperationException(
                "Identity:Bootstrap:UserName is required.");

        var adminPassword =
            configuration["Identity:Bootstrap:Password"]
            ?? throw new InvalidOperationException(
                "Identity:Bootstrap:Password is required.");

        var tokenSection =
            configuration.GetRequiredSection("Identity:Tokens");

        services.AddElsa(elsa =>
        {
            elsa.UseWorkflowManagement(management =>
                management.UseEntityFrameworkCore(
                    ef => ef.UsePostgreSql(connectionString)));

            elsa.UseWorkflowRuntime(runtime =>
                runtime.UseEntityFrameworkCore(
                    ef => ef.UsePostgreSql(connectionString)));

            elsa.UseIdentity(identity =>
            {
                identity.TokenOptions += options =>
                    tokenSection.Bind(options);

                identity.UseEntityFrameworkCore(
                    ef => ef.UsePostgreSql(connectionString));

                identity.UseDefaultAdmin(admin => admin
                    .WithAdminUserName(adminUserName)
                    .WithAdminPassword(adminPassword)
                    .WithAdminRoleName("admin")
                    .WithAdminRolePermissions(
                        new List<string> { "*" }));
            });

            elsa.UseDefaultAuthentication();
            elsa.UseScheduling();
            elsa.UseWorkflowsApi();
            
            // Code-first workflow definitions
            elsa.AddWorkflow<ShamlCaseLifecycle>();
        });

        return services;
    }
}
