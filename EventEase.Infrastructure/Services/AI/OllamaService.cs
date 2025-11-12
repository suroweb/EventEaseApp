using EventEase.Application.Interfaces;
using EventEase.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace EventEase.Infrastructure.Services.AI;

/// <summary>
/// Ollama service for running open-source models locally
/// FREE, private, and runs on your own hardware
/// Supports 100+ models: Llama 3, Mistral, Mixtral, CodeLlama, Phi, Gemma, etc.
/// </summary>
public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OllamaService> _logger;
    private readonly string _baseUrl;
    private readonly string _defaultModel;

    public AIProvider Provider => AIProvider.Ollama;

    public OllamaService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OllamaService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _defaultModel = configuration["Ollama:DefaultModel"] ?? "llama3";

        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(5); // Local models can be slow
    }

    public async Task<AIResponse> SendPromptAsync(
        string systemPrompt,
        string userPrompt,
        double temperature = 0.7,
        int maxTokens = 2000)
    {
        return await GenerateAsync(_defaultModel, userPrompt, systemPrompt, temperature, maxTokens);
    }

    public async Task<AIResponse<T>> SendPromptJsonAsync<T>(
        string systemPrompt,
        string userPrompt,
        double temperature = 0.7,
        int maxTokens = 2000) where T : class
    {
        var response = await SendPromptAsync(systemPrompt, userPrompt, temperature, maxTokens);

        if (!response.Success)
        {
            return new AIResponse<T>
            {
                Success = false,
                ErrorMessage = response.ErrorMessage,
                Content = response.Content,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens,
                Model = response.Model
            };
        }

        try
        {
            var data = JsonSerializer.Deserialize<T>(response.Content);
            return new AIResponse<T>
            {
                Success = true,
                Content = response.Content,
                Data = data,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens,
                Model = response.Model
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing Ollama JSON response");
            return new AIResponse<T>
            {
                Success = false,
                ErrorMessage = $"Failed to parse JSON: {ex.Message}",
                Content = response.Content,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens,
                Model = response.Model
            };
        }
    }

    public async Task<AIResponse> GenerateAsync(
        string model,
        string prompt,
        string? systemPrompt = null,
        double temperature = 0.7,
        int maxTokens = 2000)
    {
        try
        {
            var requestBody = new
            {
                model,
                prompt,
                system = systemPrompt,
                options = new
                {
                    temperature,
                    num_predict = maxTokens,
                    top_p = 1.0
                },
                stream = false
            };

            var response = await _httpClient.PostAsJsonAsync("/api/generate", requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>();

            if (result?.Response != null)
            {
                return new AIResponse
                {
                    Success = true,
                    Content = result.Response,
                    InputTokens = result.PromptEvalCount ?? 0,
                    OutputTokens = result.EvalCount ?? 0,
                    Model = model
                };
            }

            return new AIResponse
            {
                Success = false,
                ErrorMessage = "No response from Ollama",
                Model = model
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error connecting to Ollama. Is Ollama running?");
            return new AIResponse
            {
                Success = false,
                ErrorMessage = $"Cannot connect to Ollama at {_baseUrl}. Please ensure Ollama is running. Error: {ex.Message}",
                Model = model
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Ollama API");
            return new AIResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                Model = model
            };
        }
    }

    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string model,
        string prompt,
        string? systemPrompt = null,
        double temperature = 0.7,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model,
            prompt,
            system = systemPrompt,
            options = new
            {
                temperature,
                top_p = 1.0
            },
            stream = true
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/generate")
        {
            Content = JsonContent.Create(requestBody)
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;

            var chunk = JsonSerializer.Deserialize<OllamaGenerateResponse>(line);
            if (chunk?.Response != null)
            {
                yield return chunk.Response;
            }

            if (chunk?.Done == true)
            {
                break;
            }
        }
    }

    public async Task<float[]> GenerateEmbeddingsAsync(
        string model,
        string text)
    {
        try
        {
            var requestBody = new
            {
                model,
                prompt = text
            };

            var response = await _httpClient.PostAsJsonAsync("/api/embeddings", requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();

            return result?.Embedding ?? Array.Empty<float>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embeddings with Ollama");
            return Array.Empty<float>();
        }
    }

    public async Task<List<OllamaModel>> ListModelsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<OllamaListResponse>("/api/tags");
            return response?.Models ?? new List<OllamaModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing Ollama models");
            return new List<OllamaModel>();
        }
    }

    public async Task<bool> PullModelAsync(
        string modelName,
        IProgress<ModelPullProgress>? progress = null)
    {
        try
        {
            _logger.LogInformation("Pulling model {ModelName} from Ollama registry", modelName);

            var requestBody = new
            {
                name = modelName,
                stream = progress != null
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/pull")
            {
                Content = JsonContent.Create(requestBody)
            };

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            if (progress != null)
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var pullStatus = JsonSerializer.Deserialize<OllamaPullResponse>(line);
                    if (pullStatus != null)
                    {
                        progress.Report(new ModelPullProgress
                        {
                            Status = pullStatus.Status ?? "",
                            BytesDownloaded = pullStatus.Completed ?? 0,
                            TotalBytes = pullStatus.Total ?? 0
                        });
                    }
                }
            }

            _logger.LogInformation("Successfully pulled model {ModelName}", modelName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pulling model {ModelName}", modelName);
            return false;
        }
    }

    public async Task<bool> DeleteModelAsync(string modelName)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, "/api/delete")
            {
                Content = JsonContent.Create(new { name = modelName })
            };

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting model {ModelName}", modelName);
            return false;
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<OllamaModelInfo?> GetModelInfoAsync(string modelName)
    {
        try
        {
            var requestBody = new { name = modelName };
            var response = await _httpClient.PostAsJsonAsync("/api/show", requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaShowResponse>();

            if (result != null)
            {
                return new OllamaModelInfo
                {
                    Name = modelName,
                    Description = result.Modelfile ?? "",
                    Size = result.Size ?? 0,
                    Family = result.Details?.Family ?? "unknown",
                    Parameters = result.Details?.Parameters ?? new Dictionary<string, object>()
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model info for {ModelName}", modelName);
            return null;
        }
    }

    // Ollama API response models
    private class OllamaGenerateResponse
    {
        public string? Model { get; set; }
        public string? Response { get; set; }
        public bool Done { get; set; }
        public int? PromptEvalCount { get; set; }
        public int? EvalCount { get; set; }
    }

    private class OllamaEmbeddingResponse
    {
        public float[]? Embedding { get; set; }
    }

    private class OllamaListResponse
    {
        public List<OllamaModel>? Models { get; set; }
    }

    private class OllamaPullResponse
    {
        public string? Status { get; set; }
        public long? Completed { get; set; }
        public long? Total { get; set; }
    }

    private class OllamaShowResponse
    {
        public string? Modelfile { get; set; }
        public long? Size { get; set; }
        public OllamaModelDetails? Details { get; set; }
    }

    private class OllamaModelDetails
    {
        public string? Family { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
    }
}
