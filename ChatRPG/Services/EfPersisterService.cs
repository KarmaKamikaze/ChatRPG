using ChatRPG.Data;
using ChatRPG.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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
        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync();
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
            .Where(campaign => campaign.Id == campaignId)
            .Include(campaign => campaign.Messages)
            .Include(campaign => campaign.StartScenario)
            .Include(campaign => campaign.Environments)
            .Include(campaign => campaign.Events)
            .Include(campaign => campaign.Characters)
            .ThenInclude(character => character.CharacterAbilities)
            .ThenInclude(characterAbility => characterAbility!.Ability)
            .AsSplitQuery()
            .FirstAsync();
    }

    /// <inheritdoc />
    public async Task<List<Campaign>> GetCampaignsForUser(User user)
    {
        return await _dbContext.Campaigns
            .Where(campaign => campaign.User.Equals(user))
            .Include(campaign => campaign.Characters.Where(c => c.IsPlayer))
            .Include(campaign => campaign.StartScenario)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<StartScenario>> GetStartScenarios()
    {
        return await _dbContext.StartScenarios.ToListAsync();
    }
}
