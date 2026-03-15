using System.Text;
using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Api.Middleware;
using ContentLocalizationSaaS.Application;
using ContentLocalizationSaaS.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ProblemDetailsResultFilter>();
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddValidatorsFromAssemblyContaining<CreateProjectRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateContentItemRequestValidator>();

builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = authOptions.Jwt.Issuer,
            ValidAudience = authOptions.Jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.Jwt.SigningKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDevCors", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                "http://127.0.0.1:3000",
                "https://127.0.0.1:3000",
                "http://localhost:5173",
                "https://localhost:5173",
                "http://127.0.0.1:5173",
                "https://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();
app.UseObservabilityMiddleware();
app.UseApiExceptionMiddleware();
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseCors("LocalDevCors");
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok", service = "content-localization-api" }));

app.Run();

public partial class Program { }
