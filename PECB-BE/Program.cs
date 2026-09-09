using Carter;
using FluentValidation;
using PECB_BE.Extension;
using PECB_BE.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCarter();
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.ConfigureService(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Create/migrate the database and seed agents so a fresh clone runs as-is.
    await app.PrepareDevelopmentDatabaseAsync();
}
else
{
    // Skipped in Development on purpose: the Angular dev server proxies to the HTTP port,
    // and redirecting those calls to the HTTPS port turns them into blocked cross-origin
    // requests. Outside Development there is no proxy, so the redirect belongs back on.
    app.UseHttpsRedirection();
}

app.MapCarter();
app.Run();

