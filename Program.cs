using System.Text;
using BudgetManagementSystem.Api.Data;
using BudgetManagementSystem.Api.Enums;
using BudgetManagementSystem.Api.Helpers;
using BudgetManagementSystem.Api.Hubs;
using BudgetManagementSystem.Api.Interfaces;
using BudgetManagementSystem.Api.Middleware;
using BudgetManagementSystem.Api.Models;
using BudgetManagementSystem.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ✅ CORS: Allow React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ✅ DbContext (MySQL via Pomelo)
builder.Services.AddDbContext<AppDbContext>(opts =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection");
    opts.UseMySql(cs, ServerVersion.AutoDetect(cs));
});

// ✅ JWT Authentication
var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true
        };

        // ✅ Allow SignalR token via query string
        opts.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/budget"))
                {
                    ctx.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ✅ App services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAllocationService, AllocationService>();
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<PasswordHasher>();

// ✅ Controllers + JSON options
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = null;
});

// SignalR
builder.Services.AddSignalR();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Build app
var app = builder.Build();

// builder.Services.AddScoped<IUserService, UserService>();

// Apply migrations + seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
    if (!db.Users.Any())
    {
        var dep = new Department { Name = "Engineering" };
        db.Departments.Add(dep);
        db.Users.AddRange(
            new User
            {
                Name = "Admin",
                Email = "admin@corp.com",
                Role = UserRole.FinanceAdmin,
                PasswordHash = hasher.Hash("Admin@123")
            },
            new User
            {
                Name = "Eng Head",
                Email = "eng@corp.com",
                Role = UserRole.DepartmentHead,
                Department = dep,
                PasswordHash = hasher.Hash("Dept@123")
            },
            new User
            {
                Name = "Mgmt",
                Email = "mgmt@corp.com",
                Role = UserRole.Management,
                PasswordHash = hasher.Hash("Mgmt@123")
            }
        );
        db.SaveChanges();
    }
}

// ✅ Middleware pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// ✅ Apply CORS policy
app.UseCors("AllowFrontend");
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<BudgetHub>("/hubs/budget");

app.Run();
