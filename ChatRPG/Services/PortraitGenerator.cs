using ChatRPG.Data.Models;
using OpenAI.Images;

namespace ChatRPG.Services;

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

    public async Task GeneratePortraitAsync(Character character, string startingScenario)
    {
        var prompt = $"""
                      Create a portrait of {character.Name.Trim()}, described as {character.Description.Trim()}.

                      Let the following affect the background atmosphere and setting of this fantasy scenario:
                      {startingScenario.Trim()}

                      Style: highly detailed, cinematic lighting, fantasy digital painting, intricate textures, no text or text-boxes.
                      """;

        var image = await _imageLlmClient.GenerateImageAsync(prompt, _imageGenerationOptions);
        character.Portrait = image.Value.ImageBytes.ToArray();

        await _persistenceService.SaveAsync(character.Campaign);
    }
}
