using System.ComponentModel.DataAnnotations;
using System.Security.Authentication;
using ChatRPG.Data.Models;
using ChatRPG.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Environment = ChatRPG.Data.Models.Environment;
using CampaignModel = ChatRPG.Data.Models.Campaign;

namespace ChatRPG.Pages;

public partial class UserCampaignOverview : ComponentBase
{
    private User? User { get; set; } = null;
    private List<CampaignModel> Campaigns { get; set; } = new();
    private List<StartScenario> StartScenarios { get; set; } = new();

    [Required][BindProperty] private string CharacterName { get; set; } = "";

    [Required][BindProperty] private string CampaignTitle { get; set; } = "";

    [BindProperty] private string CustomStartScenario { get; set; } = null!;

    [Inject] private AuthenticationStateProvider? AuthProvider { get; set; }
    [Inject] private UserManager<User>? UserManager { get; set; }
    [Inject] private IPersisterService? PersisterService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authState = await AuthProvider!.GetAuthenticationStateAsync();
        User = await UserManager!.GetUserAsync(authState.User)
               ?? throw new AuthenticationException("User is not authorized");
        Campaigns = await PersisterService!.GetCampaignsForUser(User);
        Campaigns.Reverse(); // Reverse to display latest campaign first
        StartScenarios = await PersisterService.GetStartScenarios();
    }

    private async Task CreateAndStartCampaign()
    {
        if (User is null)
        {
            throw new Exception();
        }

        CampaignModel campaign = new(User, CampaignTitle, CustomStartScenario);
        Character player = new Character(campaign, CharacterType.Humanoid, CharacterName, "", true, 100);
        Environment environment = new(campaign, "Start location", "The place where it all began");
        player.Environment = environment;
        campaign.Environments.Add(environment);
        campaign.Characters.Add(player);
        await PersisterService!.SaveAsync(campaign);
        // TODO: Redirect to campaign page
        CharacterName = "";
        CampaignTitle = "";
        CustomStartScenario = null!;
    }
}
