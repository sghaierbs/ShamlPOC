using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.Authentication.ElsaIdentity.BlazorServer.Extensions;
using Elsa.Studio.Authentication.ElsaIdentity.HttpMessageHandlers;
using Elsa.Studio.Authentication.ElsaIdentity.UI.Extensions;
using Elsa.Studio.Authentication.Themes.Extensions;
using Elsa.Studio.Authentication.UI.Extensions;
using Elsa.Studio.Authentication.UI.Options;
using Elsa.Studio.Core.BlazorServer.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Designer.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

// Backend API
var backendUrl = builder.Configuration["Backend:Url"]
                 ?? throw new InvalidOperationException(
                     "Backend:Url configuration is required.");

var backend = new BackendApiConfig
{
    ConfigureBackendOptions = options =>
        options.Url = new Uri(backendUrl),

    ConfigureHttpClientBuilder = options =>
        options.AuthenticationHandler =
            typeof(ElsaIdentityAuthenticatingApiHttpMessageHandler)
};

// Classic Blazor Server hosting
builder.Services.AddRazorPages();

builder.Services.AddServerSideBlazor(options =>
{
    options.RootComponents.RegisterCustomElsaStudioElements();
    options.RootComponents.MaxJSRootComponents = 1000;
});

// Authentication
builder.Services.AddStudioAuthenticationMode(options =>
    options.Provider = StudioAuthenticationProvider.ElsaIdentity);

builder.Services.AddElsaIdentity();
builder.Services.AddElsaIdentityUI();

// Elsa Studio
builder.Services.AddCore();
builder.Services.AddShell();

builder.Services
    .AddAuthenticationUI(
        builder.Configuration.GetSection(LoginThemeOptions.SectionName))
    .AddElsaStudioLoginThemes();

builder.Services.AddRemoteBackend(backend);
builder.Services.AddWorkflowsModule();

var app = builder.Build();
app.MapStaticAssets();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();