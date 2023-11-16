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
    [Inject] private ICampaignMediatorService? CampaignMediatorService { get; set; }
    [Inject] private NavigationManager? NavMan { get; set; }

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
        Environment environment = new(campaign, "Start location", "The place where it all began");
        Character player = new(campaign, environment, CharacterType.Humanoid, CharacterName, "", true, 100);
        campaign.Environments.Add(environment);
        campaign.Characters.Add(player);
        await PersisterService!.SaveAsync(campaign);
        LaunchCampaign(campaign.Id);
    }

    private void LaunchCampaign(int id)
    {
        CampaignMediatorService!.UserCampaignDict[User!.UserName!] = id;
        NavMan!.NavigateTo("Campaign", forceLoad: true);
    }

    private void ApplyStartingScenario(string title, string scenario)
    {
        CampaignTitle = title;
        CustomStartScenario = scenario;
        StateHasChanged();
    }
}
