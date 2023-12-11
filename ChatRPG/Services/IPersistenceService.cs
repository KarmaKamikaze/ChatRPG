using ChatRPG.Data.Models;

namespace ChatRPG.Services;

/// <summary>
/// Service for persisting and loading changes from the data model.
/// </summary>
public interface IPersistenceService
{
    /// <summary>
    /// Saves the given <paramref name="campaign"/> and all its related entities.
    /// </summary>
    /// <param name="campaign">The campaign to save.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveAsync(Campaign campaign);
    /// <summary>
    /// Deletes the given <paramref name="campaign"/> and all its related entities.
    /// </summary>
    /// <param name="campaign">The campaign to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(Campaign campaign);
    /// <summary>
    /// Loads the campaign with the given <paramref name="campaignId"/> with all its related entities.
    /// </summary>
    /// <param name="campaignId">Id of the campaign to load.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Campaign> LoadFromCampaignIdAsync(int campaignId);
    /// <summary>
    /// Loads all campaigns for the given <paramref name="user"/>, along with their <see cref="StartScenario"/>s and player <see cref="Character"/>s.
    /// </summary>
    /// <param name="user">The user whose campaigns to load.</param>
    /// <returns>A list of the user's campaigns.</returns>
    Task<List<Campaign>> GetCampaignsForUser(User user);
    /// <summary>
    /// Loads all start scenarios.
    /// </summary>
    /// <returns>A list of all start scenarios.</returns>
    Task<List<StartScenario>> GetStartScenarios();
}
