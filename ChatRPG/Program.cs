using ChatRPG.API;
using ChatRPG.Areas.Identity;
using ChatRPG.Data;
using ChatRPG.Data.Models;
using ChatRPG.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
configuration.AddUserSecrets<Program>();

// Add services to the container.
var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDefaultIdentity<User>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var httpMessageHandlerFactory = new HttpMessageHandlerFactory(configuration);
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<User>>()
    .AddSingleton<HttpMessageHandler>(_ => httpMessageHandlerFactory.CreateHandler())
    .AddSingleton<IHttpClientFactory, HttpClientFactory>()
    .AddSingleton<IOpenAiLlmClient, OpenAiLlmClient>()
    .AddSingleton<IFoodWasteClient, SallingClient>()
    .AddTransient<IPersisterService, EfPersisterService>();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 4;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();

    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Initializing database with test user");
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        const string username = "test";
        var user = await userManager.FindByNameAsync(username);
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
