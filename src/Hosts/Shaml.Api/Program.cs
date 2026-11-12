
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shaml.Api.Security;
using Shaml.Application.Abstractions.Persistence;
using Shaml.Application.Abstractions.Security;
using Shaml.Application.Centers.CreateCenter;
using Shaml.Application.Centers.Management;
using Shaml.Application.Geography;
using Shaml.Application.Identity.Login;
using Shaml.Application.Identity.RegisterUser;
using Shaml.Application.Identity.Roles;
using Shaml.Application.Users.Management;
using Shaml.Infrastructure;
using Shaml.Infrastructure.Geography;
using Shaml.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// Application services
builder.Services.AddScoped<RegisterUserHandler>();
builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<UserRoleService>();
builder.Services.AddScoped<UserManagementHandler>();
builder.Services.AddScoped<IUserManagementStore, UserManagementStore>();

// Geographic lookup service
builder.Services.AddScoped<IGeographicLookupService, GeographicLookupService>();

// Center creation
builder.Services.AddScoped<CreateCenterHandler>();
builder.Services.AddScoped<ICenterCreationStore, CenterCreationStore>();
builder.Services.AddScoped<CenterManagementHandler>();
builder.Services.AddScoped<ICenterManagementStore, CenterManagementStore>();

// Current authenticated user
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// JWT generation
builder.Services.AddScoped<IAccessTokenService, JwtAccessTokenService>();




// JWT configuration
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer is not configured.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience is not configured.");

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT signing key is not configured.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must be at least 32 bytes.");
}

// Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;

                var publicIdValue = principal?
                    .FindFirst(JwtRegisteredClaimNames.Sub)?
                    .Value;

                var userRoleIdValue = principal?
                    .FindFirst("active_user_role_id")?
                    .Value;

                var tokenVersionValue = principal?
                    .FindFirst("token_version")?
                    .Value;

                if (!Guid.TryParse(
                        publicIdValue,
                        out var publicId) ||
                    !long.TryParse(
                        userRoleIdValue,
                        out var userRoleId) ||
                    !long.TryParse(
                        tokenVersionValue,
                        out var tokenVersion) ||
                    userRoleId <= 0 ||
                    tokenVersion < 0)
                {
                    context.Fail("Invalid JWT claims.");
                    return;
                }

                var dbContext = context.HttpContext
                    .RequestServices
                    .GetRequiredService<ShamlDbContext>();

                var cancellationToken =
                    context.HttpContext.RequestAborted;

                var isValid = await dbContext.UserRoles
                    .AsNoTracking()
                    .AnyAsync(
                        assignment =>
                            assignment.Id == userRoleId &&
                            assignment.IsActive &&
                            dbContext.Users.Any(user =>
                                user.Id == assignment.UserSocialId &&
                                user.PublicId == publicId &&
                                user.IsActive &&
                                user.TokenVersion == tokenVersion),
                        cancellationToken);

                if (!isValid)
                {
                    context.Fail(
                        "Token has been revoked or role is invalid.");
                }
            }
        };
    });

// Authorization
builder.Services.AddAuthorization();

builder.Services.AddSingleton<
    IAuthorizationPolicyProvider,
    PermissionPolicyProvider>();

builder.Services.AddScoped<
    IAuthorizationHandler,
    PermissionAuthorizationHandler>();

// Controllers
builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
