using EndpointApi.Configuration;
using EndpointApi.Data;
using EndpointApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection(ApiSettings.SectionName));
builder.Services.Configure<SecuritySettings>(builder.Configuration.GetSection(SecuritySettings.SectionName));
builder.Services.Configure<PolicyResponseOptions>(builder.Configuration.GetSection(PolicyResponseOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=endpoint.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IPolicyEngine, PolicyEngine>();
builder.Services.AddScoped<IRiskScoringService, RiskScoringService>();
builder.Services.AddSingleton<IEnrollmentTokenValidator, EnrollmentTokenValidator>();
builder.Services.AddSingleton<IAdminApiKeyValidator, AdminApiKeyValidator>();
builder.Services.AddScoped<IDashboardQueryService, DashboardQueryService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🟢 KESİN CORS ÇÖZÜMÜ: Her kökene, her metoda ve her başlığa izin ver!
builder.Services.AddCors(options =>
{
    options.AddPolicy("DashboardCors", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

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

// 🟢 CORS Katmanını Aktifleştir
app.UseCors("DashboardCors");

app.MapControllers();

app.Run();