using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TLALOCSG.Data;
using TLALOCSG.Models;

var builder = WebApplication.CreateBuilder(args);

/*─────────────────────────────────────────────
  Lectura de configuración
  ─────────────────────────────────────────────*/
var jwtCfg = builder.Configuration.GetSection("JWTSetting");
var connectionString = builder.Configuration.GetConnectionString("cadenaSQL");

/*─────────────────────────────────────────────
  Servicios  (Dependency-Injection)
  ─────────────────────────────────────────────*/

// DbContext
builder.Services.AddDbContext<IoTIrrigationDbContext>(opt =>
    opt.UseSqlServer(connectionString));

// Identity
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<IoTIrrigationDbContext>()
    .AddDefaultTokenProviders();

// Autenticación JWT
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.SaveToken = true;
        opt.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtCfg["ValidIssuer"],
            ValidAudience = jwtCfg["ValidAudience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtCfg["securityKey"]!))
        };
    });

// CORS – una sola política para Angular dev
builder.Services.AddCors(p => p.AddPolicy("Ng", policy =>
    policy.WithOrigins("http://localhost:4200")
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials()));

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TLALOCSG API",
        Version = "v1",
        Description = "Backend ASP.NET Core – Sistema de riego IoT"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

/*─────────────────────────────────────────────
  Build y seeding de roles
  ─────────────────────────────────────────────*/
var app = builder.Build();

// Seed roles “Admin” y “Client”
using (var scope = app.Services.CreateScope())
{
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Admin", "Client" })
        if (!await roleMgr.RoleExistsAsync(role))
            await roleMgr.CreateAsync(new IdentityRole(role));
}

/*─────────────────────────────────────────────
  Middleware pipeline
  ─────────────────────────────────────────────*/
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHsts();
app.UseHttpsRedirection();

app.UseCors("Ng");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
