using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TLALOCSG.Data;          
using TLALOCSG.Models;        // ← ApplicationUser / entidades

var builder = WebApplication.CreateBuilder(args);

/*─────────────────────────────────────────────
  1. Configuración básica y lectura de settings
  ─────────────────────────────────────────────*/
var jwtSection = builder.Configuration.GetSection("JWTSetting");
var connectionString = builder.Configuration.GetConnectionString("cadenaSQL");

/*─────────────────────────────────────────────
  2. Registro de servicios  (DI container)
  ─────────────────────────────────────────────*/

// 2.1 DbContext (SQL Server)
builder.Services.AddDbContext<IoTIrrigationDbContext>(opt =>
    opt.UseSqlServer(connectionString));

// 2.2 ASP.NET Core Identity  (usuarios / roles)
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>()          // o IdentityRole<Guid>
    .AddEntityFrameworkStores<IoTIrrigationDbContext>()
    .AddDefaultTokenProviders();

// 2.3 Autenticación JWT-Bearer
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = false;            // ↔ en prod: true
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = jwtSection["ValidAudience"],
            ValidIssuer = jwtSection["ValidIssuer"],
            IssuerSigningKey = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(
                                               jwtSection["securityKey"]!))
        };
    });

// 2.4 Controladores
builder.Services.AddControllers();

// 2.5 Swagger + esquema de seguridad Bearer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TLALOCSG API",
        Version = "v1",
        Description = "API para el sistema de riego IoT - Backend ASP.NET Core"
    });

    // Definición de seguridad
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Autorización JWT: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference   = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                },
                Scheme = "oauth2",
                Name   = "Bearer",
                In     = ParameterLocation.Header
            },
            Array.Empty<string>()
        }
    });
});

// 2.6 (Opcional) CORS para tu app Angular en http://localhost:4200
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAngular",
        policy => policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

/*─────────────────────────────────────────────
  3. Construir la aplicación
  ─────────────────────────────────────────────*/
var app = builder.Build();

/*─────────────────────────────────────────────
  4. Middleware pipeline
  ─────────────────────────────────────────────*/
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAngular");   // si configuraste la política

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
