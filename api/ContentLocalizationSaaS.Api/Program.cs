using ContentLocalizationSaaS.Api.Middleware;
using ContentLocalizationSaaS.Application;
using ContentLocalizationSaaS.Infrastructure;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddValidatorsFromAssemblyContaining<CreateProjectRequestValidator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();
app.UseApiExceptionMiddleware();
app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok", service = "content-localization-api" }));

app.Run();

public partial class Program { }
