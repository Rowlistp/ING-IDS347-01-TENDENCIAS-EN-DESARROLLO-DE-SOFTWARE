using System.Text;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using FuelTrack.Api.Data;
using FuelTrack.Api.Security;
using FuelTrack.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Key) && options.Key.Length >= 32,
        "Jwt:Key debe configurarse fuera de Git y tener al menos 32 caracteres.")
    .ValidateOnStart();

builder.Services
    .AddOptions<KeycloakOptions>()
    .Bind(builder.Configuration.GetSection(KeycloakOptions.SectionName))
    .Validate(options =>
        !options.Enabled ||
        (Uri.TryCreate(options.Authority, UriKind.Absolute, out var authority) &&
         (!options.RequireHttpsMetadata || authority.Scheme == Uri.UriSchemeHttps) &&
         !string.IsNullOrWhiteSpace(options.Audience) &&
         !string.IsNullOrWhiteSpace(options.IdentityClaim)),
        "Keycloak habilitado requiere Authority absoluta (HTTPS en producción), Audience e IdentityClaim.")
    .ValidateOnStart();

builder.Services
    .AddOptions<TicketOptions>()
    .Bind(builder.Configuration.GetSection(TicketOptions.SectionName));

var jwt = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "Falta Jwt:Key. Configúralo con user-secrets o una variable de entorno; nunca lo guardes en Git.");
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = AuthenticationSchemes.Smart;
        options.DefaultAuthenticateScheme = AuthenticationSchemes.Smart;
        options.DefaultChallengeScheme = AuthenticationSchemes.Smart;
    })
    .AddPolicyScheme(AuthenticationSchemes.Smart, AuthenticationSchemes.Smart, options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var keycloak = context.RequestServices
                .GetRequiredService<IOptions<KeycloakOptions>>().Value;
            if (!keycloak.Enabled)
                return AuthenticationSchemes.InternalJwt;

            var authorization = context.Request.Headers.Authorization.ToString();
            if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return AuthenticationSchemes.InternalJwt;

            var token = authorization["Bearer ".Length..].Trim();
            var handler = new JsonWebTokenHandler();
            if (!handler.CanReadToken(token))
                return AuthenticationSchemes.InternalJwt;

            var issuer = handler.ReadJsonWebToken(token).Issuer.TrimEnd('/');
            return string.Equals(issuer, keycloak.Authority.TrimEnd('/'), StringComparison.Ordinal)
                ? AuthenticationSchemes.Keycloak
                : AuthenticationSchemes.InternalJwt;
        };
    })
    .AddJwtBearer(AuthenticationSchemes.InternalJwt, options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var versionClaim = context.Principal?.FindFirstValue(TokenService.SecurityVersionClaim);

                if (!int.TryParse(userIdClaim, out var userId) ||
                    !int.TryParse(versionClaim, out var securityVersion))
                {
                    context.Fail("Token sin versión de seguridad válida.");
                    return;
                }

                var sessions = context.HttpContext.RequestServices
                    .GetRequiredService<SessionValidationService>();
                if (!await sessions.IsValidAsync(
                        userId,
                        securityVersion,
                        context.HttpContext.RequestAborted))
                {
                    context.Fail("La sesión fue revocada.");
                }
            }
        };
    })
    .AddJwtBearer(AuthenticationSchemes.Keycloak, options =>
    {
        options.MapInboundClaims = false;
        options.SaveToken = false;
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var keycloak = context.HttpContext.RequestServices
                    .GetRequiredService<IOptions<KeycloakOptions>>().Value;
                var resolver = context.HttpContext.RequestServices
                    .GetRequiredService<KeycloakIdentityService>();
                var localPrincipal = await resolver.ResolveAsync(
                    context.Principal!,
                    keycloak.IdentityClaim,
                    context.HttpContext.RequestAborted);

                if (localPrincipal is null)
                {
                    context.Fail("La identidad externa no corresponde a un usuario local activo.");
                    return;
                }

                context.Principal = localPrincipal;
            }
        };
    });

builder.Services
    .AddOptions<JwtBearerOptions>(AuthenticationSchemes.Keycloak)
    .Configure<IOptions<KeycloakOptions>>((options, configured) =>
    {
        var keycloak = configured.Value;
        options.Authority = keycloak.Authority;
        options.Audience = keycloak.Audience;
        options.RequireHttpsMetadata = keycloak.RequireHttpsMetadata;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = keycloak.Authority.TrimEnd('/'),
            ValidateAudience = true,
            ValidAudience = keycloak.Audience,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = keycloak.IdentityClaim,
            RoleClaimType = "__external_roles_ignored"
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<SessionValidationService>();
builder.Services.AddScoped<KeycloakIdentityService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<TicketNumberService>();
builder.Services.AddScoped<TicketQrService>();
builder.Services.AddScoped<TicketPdfService>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<SecuritySeedService>();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Introduzca únicamente el JWT."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("WebClient");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<SecuritySeedService>();
    await seeder.SeedAsync();
}

app.Run();

public partial class Program;
