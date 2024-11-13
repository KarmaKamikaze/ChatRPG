using System.ComponentModel.DataAnnotations;
using System.Security.Authentication;
using Blazored.Modal;
using Blazored.Modal.Services;
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
    private List<CampaignModel> Campaigns { get; set; } = [];
    private List<StartScenario> StartScenarios { get; set; } = [];
    private bool TestFields { get; set; }
    private int TextAreaRows { get; set; } = 6;

    [Required][BindProperty] private string CampaignTitle { get; set; } = "";
    [Required][BindProperty] private string CharacterName { get; set; } = "";
    [BindProperty] private string CharacterDescription { get; set; } = "";
    [BindProperty] private string StartScenario { get; set; } = null!;

    [Inject] private AuthenticationStateProvider? AuthProvider { get; set; }
    [Inject] private UserManager<User>? UserManager { get; set; }
    [Inject] private IPersistenceService? PersistenceService { get; set; }
    [Inject] private ICampaignMediatorService? CampaignMediatorService { get; set; }
    [Inject] private NavigationManager? NavMan { get; set; }

    [CascadingParameter] public IModalService? ConfirmDeleteModal { get; set; }

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authState = await AuthProvider!.GetAuthenticationStateAsync();
        User = await UserManager!.GetUserAsync(authState.User)
               ?? throw new AuthenticationException("User is not authorized");
        Campaigns = await PersistenceService!.GetCampaignsForUser(User);
        Campaigns.Reverse(); // Reverse to display latest campaign first
        StartScenarios = await PersistenceService.GetStartScenarios();
    }

    private async Task CreateAndStartCampaign()
    {
        if (User is null)
        {
            throw new Exception();
        }

        // Alert user if they have not set CampaignTitle or CharacterName in form
        TestFields = true;
        if (FieldIsEmpty())
        {
            return;
        }

        CampaignModel campaign = new(User, CampaignTitle, StartScenario);
        Environment environment = new(campaign, "Start location", "The place where it all began");
        Character player = new(campaign, environment, CharacterType.Humanoid, CharacterName, CharacterDescription,
            true);
        campaign.Environments.Add(environment);
        campaign.Characters.Add(player);
        await PersistenceService!.SaveAsync(campaign);
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
        StartScenario = scenario;
        StateHasChanged();
    }

    private async Task ShowCampaignDeleteModal(Campaign campaign)
    {
        IModalReference modal = ConfirmDeleteModal!.Show<ConfirmModal>("Delete Campaign",
            new ModalParameters().Add(nameof(ConfirmModal.CampaignToDelete), campaign));

        ModalResult shouldDelete = await modal.Result;
        if (shouldDelete.Confirmed)
        {
            await ModalClosed();
        }
    }

    private async Task ModalClosed()
    {
        Campaigns = await PersistenceService!.GetCampaignsForUser(User!);
        Campaigns.Reverse(); // Reverse to display latest campaign first
        StateHasChanged();
    }

    private bool FieldIsEmpty()
    {
        if (string.IsNullOrWhiteSpace(CampaignTitle) && string.IsNullOrWhiteSpace(CharacterName))
        {
            TextAreaRows = 2;
            StateHasChanged();
            return true;
        }

        if (string.IsNullOrWhiteSpace(CampaignTitle) || string.IsNullOrWhiteSpace(CharacterName))
        {
            TextAreaRows = 4;
            StateHasChanged();
            return true;
        }

        return false;
    }

    private void UpdateCampaignTitleOnKeyPress(ChangeEventArgs e)
    {
        if (e.Value != null) CampaignTitle = e.Value.ToString()!;
        AdjustAlerts();
    }

    private void UpdateCharacterNameOnKeyPress(ChangeEventArgs e)
    {
        if (e.Value != null) CharacterName = e.Value.ToString()!;
        AdjustAlerts();
    }

    private void AdjustAlerts()
    {
        if (!TestFields) return;
        if (!string.IsNullOrWhiteSpace(CampaignTitle) && !string.IsNullOrWhiteSpace(CharacterName))
        {
            TextAreaRows = 6;
        }
        else if (!string.IsNullOrWhiteSpace(CampaignTitle) || !string.IsNullOrWhiteSpace(CharacterName))
        {
            TextAreaRows = 4;
        }
        else
        {
            TextAreaRows = 2;
        }

        StateHasChanged();
    }
}
