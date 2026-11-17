using FlowDesk.Application.Contracts.Persistence;
using FlowDesk.Application.Contracts.Services;
using FlowDesk.Application.Services;
using FlowDesk.Infrastructure.Identity;
using FlowDesk.Infrastructure.Persistence;
using FlowDesk.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString =
    builder.Configuration.GetConnectionString("FlowDeskConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'FlowDeskConnection' was not found.");

builder.Services.AddDbContext<FlowDeskDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddDbContext<FlowDeskIdentityDbContext>(options =>
{
    options.UseSqlServer(
        connectionString,
        sql => sql.MigrationsHistoryTable(
            "__IdentityMigrationsHistory"));
});

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(5);

        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<FlowDeskIdentityDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";

    options.Cookie.Name = "FlowDesk.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;

    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddScoped(
    typeof(IRepository<>),
    typeof(Repository<>));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IWorkItemService, WorkItemService>();
builder.Services.AddScoped<ITeamMemberService, TeamMemberService>();
builder.Services.AddScoped<IProjectMemberService, ProjectMemberService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();