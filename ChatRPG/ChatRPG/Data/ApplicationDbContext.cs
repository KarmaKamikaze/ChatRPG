using System.Diagnostics.CodeAnalysis;
using ChatRPG.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Environment = ChatRPG.Data.Models.Environment;

namespace ChatRPG.Data;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
public sealed class ApplicationDbContext : IdentityDbContext<User>
{
    public DbSet<StartScenario> StartScenarios { get; private set; } = null!;
    public DbSet<Campaign> Campaigns { get; private set; } = null!;
    public DbSet<Character> Characters { get; private set; } = null!;
    public DbSet<Event> Events { get; private set; } = null!;
    public DbSet<Environment> Environments { get; private set; } = null!;
    public DbSet<CharacterEnvironment> CharacterEnvironments { get; private set; } = null!;
    public DbSet<Ability> Abilities { get; private set; } = null!;
    public DbSet<CharacterAbility> CharacterAbilities { get; private set; } = null!;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<CharacterAbility>(builder =>
            {
                builder.HasKey(c => new { c.CharacterId, c.AbilityId });
                builder.HasOne(c => c.Character).WithMany(c => c.CharacterAbilities);
            })
            .Entity<CharacterEnvironment>(builder =>
            {
                builder.HasKey(x => new { x.CharacterId, x.Version });
                builder.Property(p => p.Version).HasDefaultValue(1);
                builder.HasOne(c => c.Character).WithMany(c => c.CharacterEnvironments);
            })
            .Entity<Event>(builder => builder.Property(x => x.Ordering).HasDefaultValue(1))
            ;
    }
}
