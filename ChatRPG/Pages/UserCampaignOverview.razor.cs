using System.ComponentModel.DataAnnotations;
using System.Security.Authentication;
using Blazored.Modal;
using Blazored.Modal.Services;
using ChatRPG.Data.Models;
using ChatRPG.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CampaignModel = ChatRPG.Data.Models.Campaign;
using Environment = ChatRPG.Data.Models.Environment;

namespace ChatRPG.Pages;

public partial class UserCampaignOverview : ComponentBase
{
    private User? User { get; set; } = null;
    private List<CampaignModel> Campaigns { get; set; } = [];
    private List<StartScenario> StartScenarios { get; set; } = [];
    private bool TestFields { get; set; }
    private int TextAreaRows { get; set; } = 6;
    private bool IsOpenWorld { get; set; } = true;
    private bool IsProcessingPdfFile { get; set; } = false;
    private byte[]? UploadedFile { get; set; }
    private string FileUploadError { get; set; } = string.Empty;

    [Required]
    [BindProperty]
    private string CampaignTitle { get; set; } = "";

    [Required]
    [BindProperty]
    private string CharacterName { get; set; } = "";

    [BindProperty]
    private string CharacterDescription { get; set; } = "";

    [BindProperty]
    private string StartScenario { get; set; } = null!;

    [Inject]
    private AuthenticationStateProvider? AuthProvider { get; set; }

    [Inject]
    private UserManager<User>? UserManager { get; set; }

    [Inject]
    private IPersistenceService? PersistenceService { get; set; }

    [Inject]
    private VisualizationService? VisualizationService { get; set; }

    [Inject]
    private ICampaignMediatorService? CampaignMediatorService { get; set; }

    [Inject]
    private NavigationManager? NavMan { get; set; }

    [Inject]
    private ScenarioDocumentService? ScenarioDocumentService { get; set; }

    [Inject]
    private ReActScribeAgent? ReActScribeAgent { get; set; }

    [CascadingParameter]
    public IModalService? ConfirmDeleteModal { get; set; }

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
        await PersistenceService!.SaveAsync(campaign); // Save the campaign ID to the database

        if (!IsOpenWorld)
        {
            // Upload campaign documents to vector database
            // UploadedFile should not be able to be null since the button is disabled if it is
            await ScenarioDocumentService!.StoreScenarioEmbedding(campaign.Id, UploadedFile!);

            campaign.NarrativeGraph = await ReActScribeAgent!.ScribeNarrativeGraph(UploadedFile!);

            campaign.StartScenario = await ScenarioDocumentService.GenerateStartingScenario(campaign);
            await PersistenceService!.SaveAsync(campaign);
            VisualizationService!.VisualizeNarrativeGraphIfEnabled(campaign.NarrativeGraph);
        }

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

    private void OnCampaignTypeChange(bool value)
    {
        IsOpenWorld = value;
        IsProcessingPdfFile = false;
        FileUploadError = string.Empty;
        UploadedFile = null;
    }

    private async Task HandleScenarioFileUpload(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            FileUploadError = "Only PDF files are allowed.";
            UploadedFile = null;
        }
        else
        {
            IsProcessingPdfFile = true;
            FileUploadError = string.Empty;

            try
            {
                using var memoryStream = new MemoryStream();
                await file.OpenReadStream(maxAllowedSize: long.MaxValue).CopyToAsync(memoryStream);
                UploadedFile = memoryStream.ToArray();
            }
            finally
            {
                IsProcessingPdfFile = false;
            }
        }
    }
}
