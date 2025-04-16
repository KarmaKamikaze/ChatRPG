using ChatRPG.Data.Models;
using OpenAI.Images;

namespace ChatRPG.Services;

/// <summary>
/// Service for generating character portraits using the DALL-E 3 model.
/// </summary>
public class PortraitGenerator
{
    private readonly ImageClient _imageLlmClient;
    private readonly ImageGenerationOptions _imageGenerationOptions;
    private readonly IPersistenceService _persistenceService;

    public PortraitGenerator(IConfiguration configuration, IPersistenceService persistenceService)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI"));
        _imageLlmClient = new ImageClient("dall-e-3", configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!);
        _imageGenerationOptions = new ImageGenerationOptions()
        {
            Quality = GeneratedImageQuality.Standard,
            Size = GeneratedImageSize.W1024xH1024,
            Style = GeneratedImageStyle.Vivid,
            ResponseFormat = GeneratedImageFormat.Bytes
        };
        _persistenceService = persistenceService;
    }

    /// <summary>
    /// Generates a portrait of the given character using the DALL-E 3 model.
    /// </summary>
    /// <param name="character">The character that will have their portrait generated.</param>
    /// <param name="atmosphereDescription">The background atmosphere description.</param>
    public async Task GeneratePortraitAsync(Character character, string atmosphereDescription)
    {
        var prompt = $"""
                      Create a portrait of {character.Name.Trim()}, described as {character.Description.Trim()}.

                      Let the following affect the background atmosphere and setting of this fantasy scenario:
                      {atmosphereDescription.Trim()}

                      Style: highly detailed, cinematic lighting, fantasy digital painting, intricate textures, no text or text-boxes.
                      """;

        var image = await _imageLlmClient.GenerateImageAsync(prompt, _imageGenerationOptions);
        character.Portrait = image.Value.ImageBytes.ToArray();

        await _persistenceService.SaveAsync(character.Campaign);
    }
}
