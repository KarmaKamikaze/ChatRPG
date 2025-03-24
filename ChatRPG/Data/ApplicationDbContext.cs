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
    public DbSet<NarrativeGraph> NarrativeGraphs { get; private set; } = null!;
    public DbSet<NarrativeNode> NarrativeNodes { get; private set; } = null!;
    public DbSet<NarrativeEdge> NarrativeEdges { get; private set; } = null!;


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<NarrativeEdge>()
            .Ignore(edge => edge.SourceNodeName)
            .Ignore(edge => edge.TargetNodeName)
            .HasOne(edge => edge.TargetNode)
            .WithMany()
            .HasForeignKey(edge => edge.TargetNodeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<NarrativeEdge>()
            .Ignore(edge => edge.SourceNodeName)
            .Ignore(edge => edge.TargetNodeName)
            .HasOne(edge => edge.SourceNode)
            .WithMany(node => node.Edges)
            .HasForeignKey(edge => edge.SourceNodeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
