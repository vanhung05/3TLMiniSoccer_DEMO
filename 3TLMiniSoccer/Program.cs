using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Hubs;
using _3TLMiniSoccer.Services;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Program>())
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Add SignalR
builder.Services.AddSignalR();
builder.Services.AddScoped<NotificationHubService>();


// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), 
        sqlOptions => 
        {
            sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            sqlOptions.CommandTimeout(30);
        }));

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Add custom services
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<FieldService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<FieldAdminService>();
builder.Services.AddScoped<AccountAdminService>();
builder.Services.AddScoped<CategoryAdminService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<ImageService>();

// Add payment configuration services
builder.Services.AddScoped<PaymentConfigService>();
builder.Services.AddScoped<SEPayService>();
builder.Services.AddScoped<PaymentOrderService>();

// Add system configuration service
builder.Services.AddScoped<SystemConfigService>();

// Add cart service
builder.Services.AddScoped<CartService>();

// Add voucher service
builder.Services.AddScoped<VoucherService>();
builder.Services.AddScoped<VoucherAdminService>();

builder.Services.AddScoped<ContactAdminService>();
builder.Services.AddScoped<BookingScheduleService>();
builder.Services.AddScoped<OrderAdminService>();
builder.Services.AddScoped<BookingStatisticsService>();
builder.Services.AddScoped<SalesStatisticsService>();

// Add session service
builder.Services.AddScoped<SessionService>();

// Add dashboard service
builder.Services.AddScoped<DashboardService>();


// Add HttpClient for external API calls
builder.Services.AddHttpClient();

// Add Data Protection (required for OAuth state)
builder.Services.AddDataProtection();

// Add session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax; // Important for OAuth
});

// Add OAuth service
builder.Services.AddScoped<OAuthService>();

// Add authentication
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.SameSite = SameSiteMode.Lax; // Important for OAuth
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Allow HTTP in development
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["OAuth:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["OAuth:Google:ClientSecret"] ?? "";
        // Keep using custom callback path that matches Google Console
        options.CallbackPath = "/Account/GoogleCallback";
        options.SaveTokens = true;
        
        // Critical: Configure CorrelationCookie to fix "oauth state missing" error
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.CorrelationCookie.HttpOnly = true;
        options.CorrelationCookie.IsEssential = true;
    })
    .AddFacebook(options =>
    {
        options.AppId = builder.Configuration["OAuth:Facebook:AppId"] ?? "";
        options.AppSecret = builder.Configuration["OAuth:Facebook:AppSecret"] ?? "";
        options.CallbackPath = "/Account/FacebookCallback";
        options.SaveTokens = true;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax; // Fix OAuth state issue
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StaffOrAdmin", policy => policy.RequireRole("Staff", "Admin"));
    options.AddPolicy("UserOrAbove", policy => policy.RequireRole("User", "Staff", "Admin"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// TEMPORARY: Always show developer exception page for debugging
app.UseDeveloperExceptionPage();
/*if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}*/

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NotificationHub>("/notificationHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Ensure database is created and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
    
}

app.Run();
