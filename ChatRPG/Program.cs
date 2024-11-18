using Blazored.Modal;
using ChatRPG.API;
using ChatRPG.Areas.Identity;
using ChatRPG.Data;
using ChatRPG.Data.Models;
using ChatRPG.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;
configuration.AddUserSecrets<Program>();

// Add services to the container.
string connectionString = configuration.GetConnectionString("DefaultConnection") ??
                          throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDefaultIdentity<User>()
    .AddSignInManager<SignInManager<User>>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddBlazoredModal();

builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<User>>()
    .AddTransient<IReActLlmClient, ReActLlmClient>()
    .AddScoped<IPersistenceService, EfPersistenceService>()
    .AddTransient<IEmailSender, EmailSender>()
    .AddTransient<GameInputHandler>()
    .AddTransient<GameStateManager>()
    .AddSingleton<ICampaignMediatorService, CampaignMediatorService>()
    .AddScoped<JsInteropService>();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 4;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();

    using IServiceScope scope = app.Services.CreateScope();
    ILogger<Program> logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Initializing database with test user");
    try
    {
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();

        UserManager<User> userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        const string username = "test";
        User? user = await userManager.FindByNameAsync(username);
        if (user == null)
        {
            user = new User(username)
            {
                Email = username,
                EmailConfirmed = true,
                LockoutEnabled = false
            };
            await userManager.CreateAsync(user, password: username);
        }

        logger.LogInformation("Database was successfully initialized");
    }
    catch (Exception e)
    {
        logger.LogError(e, "An error occurred while initializing database");
    }
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
