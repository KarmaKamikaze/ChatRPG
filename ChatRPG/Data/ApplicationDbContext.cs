using ChatRPG.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Environment = ChatRPG.Data.Models.Environment;

namespace ChatRPG.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User>(options)
{
    public DbSet<StartScenario> StartScenarios { get; private set; } = null!;
    public DbSet<Campaign> Campaigns { get; private set; } = null!;
    public DbSet<Character> Characters { get; private set; } = null!;
    public DbSet<Environment> Environments { get; private set; } = null!;
}
