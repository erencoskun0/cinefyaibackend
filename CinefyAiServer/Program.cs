using Scalar.AspNetCore;
using CinefyAiServer.Data;
using Microsoft.EntityFrameworkCore;
using CinefyAiServer.Entities;
using CinefyAiServer.Configurations;
using CinefyAiServer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "CinefyAI API",
            Version = "v1",
            Description = "CinefyAI Sinema Rezervasyon Sistemi API Dokümantasyonu",
            Contact = new()
            {
                Name = "CinefyAI Team",
                Email = "info@cinefy.ai",
                Url = new("https://cinefy.ai")
            }
        };
        return Task.CompletedTask;
    });
});

// Add API Explorer services for better documentation
builder.Services.AddEndpointsApiExplorer();

// MSSQL bağlantısı için DbContext servisini ekle
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity servisleri
builder.Services.AddIdentity<User, Role>(options =>
{
    
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    options.User.RequireUniqueEmail = true;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// JWT ayarlarını yapılandır
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName);
builder.Services.Configure<JwtSettings>(jwtSettings);

var jwtSettingsObject = jwtSettings.Get<JwtSettings>();
var key = Encoding.UTF8.GetBytes(jwtSettingsObject?.SecretKey ?? throw new InvalidOperationException("JWT SecretKey not found"));

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettingsObject.Issuer,
        ValidAudience = jwtSettingsObject.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

// Authorization
builder.Services.AddAuthorization();

// Custom servisler
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IFileService, FileService>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "CinefyAI API";
        options.Theme = ScalarTheme.Moon;
        options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
        options.CustomCss = "";
     
    });
}

app.UseHttpsRedirection();

app.UseStaticFiles();



app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
