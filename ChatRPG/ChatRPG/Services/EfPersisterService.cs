using ChatRPG.Data;
using ChatRPG.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatRPG.Services;

/// <summary>
/// Service for persisting and loading changes from the data model using Entity Framework.
/// </summary>
public class EfPersisterService : IPersisterService
{
    private readonly ILogger<EfPersisterService> _logger;
    private readonly ApplicationDbContext _dbContext;

    public EfPersisterService(ILogger<EfPersisterService> logger, ApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task SaveAsync(Campaign campaign)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            if (!_dbContext.Campaigns.Contains(campaign))
            {
                _dbContext.Campaigns.Add(campaign);
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while saving");
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task<Campaign> LoadFromCampaignIdAsync(int campaignId)
    {
        return await _dbContext.Campaigns
            .Where(cam => cam.Id == campaignId)
            .Include(cam => cam.Abilities)
            .Include(cam => cam.Environments)
            .Include(cam => cam.Events)
            .Include(cam => cam.Characters)
            .ThenInclude(c => c.CharacterAbilities)
            .Include(cam => cam.Characters)
            .ThenInclude(c => c.CharacterEnvironments)
            .AsSplitQuery()
            .FirstAsync();
    }
}
