using EndpointApi.Configuration;
using EndpointApi.Data;
using EndpointApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection(ApiSettings.SectionName));
builder.Services.Configure<SecuritySettings>(builder.Configuration.GetSection(SecuritySettings.SectionName));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=endpoint.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IPolicyEngine, PolicyEngine>();
builder.Services.AddScoped<IRiskScoringService, RiskScoringService>();
builder.Services.AddSingleton<IEnrollmentTokenValidator, EnrollmentTokenValidator>();
builder.Services.AddScoped<IDashboardQueryService, DashboardQueryService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var apiSettings = builder.Configuration.GetSection(ApiSettings.SectionName).Get<ApiSettings>() ?? new ApiSettings();
if (apiSettings.CorsOrigins is { Length: > 0 })
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DashboardCors", policy =>
            policy.WithOrigins(apiSettings.CorsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod());
    });
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DbInitializer.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (apiSettings.CorsOrigins is { Length: > 0 })
    app.UseCors("DashboardCors");

app.MapControllers();

app.Run();
