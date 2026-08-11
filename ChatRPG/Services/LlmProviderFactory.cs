using LangChain.Providers;
using LangChain.Providers.Ollama;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;

namespace ChatRPG.Services;

/// <summary>
/// Builds LangChain chat and embedding models from configuration, switching between OpenAI and a
/// locally hosted Ollama model based on the "LlmSettings:Provider" setting. This is the single place
/// that knows how to construct a provider/model pair, so call sites never branch on provider themselves.
/// </summary>
public class LlmProviderFactory
{
    private readonly IConfiguration _configuration;
    private readonly bool _useOllama;
    private readonly Lazy<IProvider> _provider;

    public LlmProviderFactory(IConfiguration configuration)
    {
        _configuration = configuration;

        var providerName = configuration.GetSection("LlmSettings").GetValue<string>("Provider");
        ArgumentException.ThrowIfNullOrEmpty(providerName);
        _useOllama = providerName.Equals("Ollama", StringComparison.OrdinalIgnoreCase);

        if (_useOllama)
        {
            ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("OllamaSettings").GetValue<string>("BaseUrl"));
        }
        else
        {
            ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI"));
        }

        _provider = new Lazy<IProvider>(CreateProvider);
    }

    private IProvider CreateProvider()
    {
        if (_useOllama)
        {
            var baseUrl = _configuration.GetSection("OllamaSettings").GetValue<string>("BaseUrl")!;
            return new OllamaProvider(baseUrl);
        }

        var apiKey = _configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!;
        return new OpenAiProvider(apiKey);
    }

    public ChatModel CreateChatModel(double temperature, bool useStreaming = false)
    {
        if (_useOllama)
        {
            var modelId = _configuration.GetSection("OllamaSettings").GetValue<string>("ChatModel");
            ArgumentException.ThrowIfNullOrEmpty(modelId);

            return new OllamaChatModel((OllamaProvider)_provider.Value, modelId)
            {
                Settings = new OllamaChatSettings { UseStreaming = useStreaming, Temperature = (float)temperature }
            };
        }

        return new Gpt4OmniModel((OpenAiProvider)_provider.Value)
        {
            Settings = new OpenAiChatSettings { UseStreaming = useStreaming, Temperature = temperature }
        };
    }

    /// <summary>
    /// Creates an embedding model for RAG storage/retrieval. <paramref name="dimensions"/> is the
    /// vector size the chosen model produces; it must match whatever the vector collection was created
    /// with, so always read it from here rather than hardcoding it at call sites.
    /// </summary>
    public IEmbeddingModel CreateEmbeddingModel(out int dimensions)
    {
        if (_useOllama)
        {
            var ollamaConfig = _configuration.GetSection("OllamaSettings");
            var modelId = ollamaConfig.GetValue<string>("EmbeddingModel");
            ArgumentException.ThrowIfNullOrEmpty(modelId);

            dimensions = ollamaConfig.GetValue<int?>("EmbeddingDimensions")
                         ?? throw new ArgumentException(
                             "OllamaSettings:EmbeddingDimensions must be set to the chosen embedding model's vector size.");

            return new OllamaEmbeddingModel((OllamaProvider)_provider.Value, modelId);
        }

        dimensions = 1536;
        return new TextEmbeddingV3SmallModel((OpenAiProvider)_provider.Value);
    }
}
