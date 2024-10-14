using ChatRPG.Data;
using ChatRPG.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ChatRPG.Services;

/// <summary>
/// Service for persisting and loading changes from the data model using Entity Framework.
/// </summary>
public class EfPersistenceService(ILogger<EfPersistenceService> logger, ApplicationDbContext dbContext)
    : IPersistenceService
{
    /// <inheritdoc />
    public async Task SaveAsync(Campaign campaign)
    {
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            if (!(await dbContext.Campaigns.ContainsAsync(campaign)))
            {
                await dbContext.Campaigns.AddAsync(campaign);
            }

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            logger.LogInformation("Saved campaign with id {Id} successfully", campaign.Id);
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred while saving");
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Campaign campaign)
    {
        if (!(await dbContext.Campaigns.ContainsAsync(campaign)))
        {
            return;
        }

        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            int campaignId = campaign.Id;
            dbContext.Campaigns.Remove(campaign);

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            logger.LogInformation("Deleted campaign with id {Id} successfully", campaignId);
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred while deleting");
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Campaign> LoadFromCampaignIdAsync(int campaignId)
    {
        return await dbContext.Campaigns
            .Where(campaign => campaign.Id == campaignId)
            .Include(campaign => campaign.Messages)
            .Include(campaign => campaign.Environments)
            .Include(campaign => campaign.Characters)
            .ThenInclude(character => character.CharacterAbilities)
            .ThenInclude(characterAbility => characterAbility!.Ability)
            .AsSplitQuery()
            .FirstAsync();
    }

    /// <inheritdoc />
    public async Task<List<Campaign>> GetCampaignsForUser(User user)
    {
        return await dbContext.Campaigns
            .Where(campaign => campaign.User.Equals(user))
            .Include(campaign => campaign.Characters.Where(c => c.IsPlayer))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<StartScenario>> GetStartScenarios()
    {
        return await dbContext.StartScenarios.ToListAsync();
    }
}
