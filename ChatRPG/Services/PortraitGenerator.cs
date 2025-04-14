using ChatRPG.Data.Models;
using OpenAI.Images;

namespace ChatRPG.Services;

public class PortraitGenerator
{
    private readonly ImageClient _llm;
    private readonly ImageGenerationOptions _imageGenerationOptions;
    private readonly IPersistenceService _persistenceService;

    public PortraitGenerator(IConfiguration configuration, IPersistenceService persistenceService)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI"));
        _llm = new ImageClient("dall-e-3", configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!);
        _imageGenerationOptions = new ImageGenerationOptions()
        {
            Quality = GeneratedImageQuality.Standard,
            Size = GeneratedImageSize.W1024xH1792,
            Style = GeneratedImageStyle.Vivid,
            ResponseFormat = GeneratedImageFormat.Bytes
        };
        _persistenceService = persistenceService;
    }

    public async Task GeneratePortraitAsync(Character character, string startingScenario)
    {
        var prompt = $"""
                      A portrait of {character.Name.Trim()}, described as {character.Description.Trim()}.

                      The background reflects the atmosphere and setting of this fantasy scenario:
                      {startingScenario.Trim()}

                      Style: highly detailed, cinematic lighting, fantasy digital painting, intricate textures, professional concept art quality.
                      """;

        var image = await _llm.GenerateImageAsync(prompt, _imageGenerationOptions);
        character.Portrait = image.Value.ImageBytes.ToArray();

        await _persistenceService.SaveAsync(character.Campaign);
    }
}
