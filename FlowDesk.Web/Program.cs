using FlowDesk.Application.Contracts.Persistence;
using FlowDesk.Application.Contracts.Services;
using FlowDesk.Application.Services;
using FlowDesk.Infrastructure.Persistence;
using FlowDesk.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();


var connectionString =
    builder.Configuration.GetConnectionString("FlowDeskConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'FlowDeskConnection' was not found.");

builder.Services.AddDbContext<FlowDeskDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IWorkItemService, WorkItemService>();
builder.Services.AddScoped<ITeamMemberService, TeamMemberService>();
builder.Services.AddScoped<IProjectMemberService, ProjectMemberService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
