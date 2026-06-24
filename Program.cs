using Amazon.S3;
using Amazon.Runtime;
using AssesmentReservas.API.Data;
using AssesmentReservas.API.Enums;
using AssesmentReservas.API.Models;
using AssesmentReservas.API.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Logging: Serilog (consola + archivo).
// ---------------------------------------------------------------------------
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .WriteTo.File("logs/reservas-.log", rollingInterval: RollingInterval.Day));

// ---------------------------------------------------------------------------
// Settings (Options pattern).
// ---------------------------------------------------------------------------
builder.Services.Configure<MinioSettings>(builder.Configuration.GetSection(MinioSettings.SectionName));
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection(MailSettings.SectionName));
builder.Services.Configure<KycSettings>(builder.Configuration.GetSection(KycSettings.SectionName));

// ---------------------------------------------------------------------------
// Base de datos: PostgreSQL + EF Core.
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// ---------------------------------------------------------------------------
// Identity + autenticación por cookies.
// ---------------------------------------------------------------------------
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";

    // Para llamadas de API devolvemos códigos de estado en vez de redirigir.
    options.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
});

// ---------------------------------------------------------------------------
// Cache distribuido: Redis.
// ---------------------------------------------------------------------------
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "reservas:";
});

// ---------------------------------------------------------------------------
// Almacenamiento de objetos: MinIO (vía AWS SDK S3, path-style).
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var cfg = builder.Configuration.GetSection(MinioSettings.SectionName).Get<MinioSettings>()!;
    var s3Config = new AmazonS3Config
    {
        ServiceURL = cfg.Endpoint,
        ForcePathStyle = true,
        UseHttp = !cfg.UseSsl
    };
    return new AmazonS3Client(new BasicAWSCredentials(cfg.AccessKey, cfg.SecretKey), s3Config);
});

// ---------------------------------------------------------------------------
// Servicios de aplicación (lógica de negocio por módulo).
// ---------------------------------------------------------------------------
builder.Services.AddScoped<AssesmentReservas.API.Interfaces.Identity.IAuthService,
    AssesmentReservas.API.Services.Identity.AuthService>();
builder.Services.AddSingleton<AssesmentReservas.API.Interfaces.Infrastructure.IFileStorageService,
    AssesmentReservas.API.Services.Infrastructure.MinioStorageService>();
builder.Services.AddScoped<AssesmentReservas.API.Interfaces.Properties.IPropertyService,
    AssesmentReservas.API.Services.Properties.PropertyService>();
builder.Services.AddScoped<AssesmentReservas.API.Interfaces.Properties.IFavoriteService,
    AssesmentReservas.API.Services.Properties.FavoriteService>();
builder.Services.AddScoped<AssesmentReservas.API.Interfaces.Bookings.IBookingService,
    AssesmentReservas.API.Services.Bookings.BookingService>();
builder.Services.AddSingleton<AssesmentReservas.API.Interfaces.Notifications.IEmailSender,
    AssesmentReservas.API.Services.Notifications.MailKitEmailSender>();
builder.Services.AddScoped<AssesmentReservas.API.Interfaces.Notifications.INotificationService,
    AssesmentReservas.API.Services.Notifications.NotificationService>();
builder.Services.AddSingleton<AssesmentReservas.API.Interfaces.Infrastructure.IEncryptionService,
    AssesmentReservas.API.Services.Infrastructure.AesEncryptionService>();
builder.Services.AddSingleton<AssesmentReservas.API.Interfaces.Infrastructure.IOcrService,
    AssesmentReservas.API.Services.Infrastructure.TesseractOcrService>();
builder.Services.AddScoped<AssesmentReservas.API.Interfaces.Identity.IKycService,
    AssesmentReservas.API.Services.Identity.KycService>();
builder.Services.AddScoped<AssesmentReservas.API.Interfaces.Owner.IDashboardService,
    AssesmentReservas.API.Services.Owner.DashboardService>();
builder.Services.AddScoped<AssesmentReservas.API.Interfaces.Owner.IReportService,
    AssesmentReservas.API.Services.Owner.ExcelReportService>();

// Job en segundo plano: recordatorios de llegada/salida + cierre de estancias.
builder.Services.AddHostedService<AssesmentReservas.API.Services.Notifications.BookingReminderService>();

// ---------------------------------------------------------------------------
// MVC (Razor Views) + API.
// ---------------------------------------------------------------------------
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Migraciones automáticas + seed de roles al arrancar.
// ---------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in Roles.All)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}

// ---------------------------------------------------------------------------
// Pipeline HTTP.
// ---------------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
