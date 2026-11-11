using AppointmentBooking.API.BackgroundServices;
using AppointmentBooking.API.Identity;
using AppointmentBooking.API.Security;
using Microsoft.OpenApi;
using AppointmentBooking.Core.Interfaces.Appointments;
using AppointmentBooking.Core.Interfaces.Notifications;
using AppointmentBooking.Infrastructure.Appointments;
using AppointmentBooking.Infrastructure.Data;
using AppointmentBooking.Infrastructure.Identity;
using AppointmentBooking.Infrastructure.Notifications;
using AppointmentBooking.Infrastructure.Notifications.AccessCodes;
using AppointmentBooking.Infrastructure.Notifications.Email;
using AppointmentBooking.Infrastructure.Notifications.Sms;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
// Database configuration
builder.Services.AddDbContext<AppointmentBookingDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        });
});

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);

        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppointmentBookingDbContext>()
    .AddSignInManager();

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException(
        "JWT signing secret is not configured.");

var jwtKey = Encoding.UTF8.GetBytes(jwtSecret);

if (jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT signing secret must contain at least 32 bytes.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer is not configured.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience is not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
});

builder.Services.AddAuthorization();

// Controllers
builder.Services.AddControllers();
builder.Services.Configure<IdentityDocumentSecurityOptions>(
    builder.Configuration.GetSection(
        IdentityDocumentSecurityOptions.SectionName));

builder.Services.AddSingleton<
    IIdentityDocumentSecurityService,
    IdentityDocumentSecurityService>();
builder.Services.AddScoped<
    INotificationProcessingService,
    NotificationProcessingService>();
builder.Services.Configure<ClickatellOptions>(
    builder.Configuration.GetSection(
        ClickatellOptions.SectionName));
builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection(
        SmtpOptions.SectionName));

builder.Services.AddScoped<
    IEmailNotificationSender,
    SmtpEmailNotificationSender>();

builder.Services.AddHttpClient<
    ISmsNotificationSender,
    ClickatellSmsNotificationSender>();
builder.Services.Configure<AppointmentAccessCodeOptions>(
    builder.Configuration.GetSection(
        AppointmentAccessCodeOptions.SectionName));

builder.Services.AddScoped<
    IAppointmentAccessCodeService,
    AppointmentAccessCodeService>();
builder.Services.AddScoped<
    IAppointmentAccessSessionService,
    AppointmentAccessSessionService>();
builder.Services.AddScoped<
    RoleManager<IdentityRole>>();

builder.Services.AddHostedService<NotificationBackgroundService>();
builder.Services.AddDataProtection()
    .SetApplicationName("AppointmentBooking");
// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT access token."
        });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(
    "Bearer", document)] = new List<string>()
        });
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

var app = builder.Build();
await IdentityDataSeeder.SeedAsync(app.Services);

await IdentityAdminSeeder.SeedAsync(
    app.Services,
    app.Configuration);
await IdentityStaffSeeder.SeedAsync(
    app.Services,
    app.Configuration);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
