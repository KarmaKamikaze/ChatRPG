using ChatRPG.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Environment = ChatRPG.Data.Models.Environment;

namespace ChatRPG.Data;
public sealed class ApplicationDbContext : IdentityDbContext<User>
{
    public DbSet<StartScenario> StartScenarios { get; set; } = null!;
    public DbSet<Campaign> Campaigns { get; set; } = null!;
    public DbSet<Character> Characters { get; set; } = null!;
    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<Environment> Environments { get; set; } = null!;
    public DbSet<CharacterLocation> CharacterLocations { get; set; } = null!;
    public DbSet<Ability> Abilities { get; set; } = null!;
    public DbSet<CharacterAbility> CharacterAbilities { get; set; } = null!;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<CharacterAbility>(builder => builder.HasKey(c => new { c.CharacterId, c.AbilityId }))
            .Entity<CharacterLocation>(builder =>
            {
                builder.HasKey(x => new { x.CharacterId, x.Version });
                builder.Property(p => p.Version).HasDefaultValue(1);
            })
            .Entity<Event>(builder => builder.Property(x => x.Ordering).HasDefaultValue(1))
            ;
    }
}
