using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FocusDesk.Ubuntu.Services;

public class OllamaModelInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class OllamaTagsResponse
{
    [JsonPropertyName("models")]
    public List<OllamaModelInfo> Models { get; set; } = new();
}

public class OllamaGenerateRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;
}

public class OllamaGenerateResponse
{
    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;
}

public class LocalLlmSummarizer : IDisposable
{
    private readonly HttpClient _httpClient;
    private const string OllamaUrl = "http://localhost:11434";

    public LocalLlmSummarizer()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(OllamaUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(15); // Inference can take some time
    }

    public bool IsModelAvailable()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/tags");
            var response = _httpClient.Send(request);
            if (response.IsSuccessStatusCode)
            {
                using var stream = response.Content.ReadAsStream();
                var tags = JsonSerializer.Deserialize<OllamaTagsResponse>(stream);
                return tags != null && tags.Models.Count > 0;
            }
        }
        catch
        {
            // Ollama not running or no models
        }
        return false;
    }

    private async Task<string> GetFirstModelNameAsync()
    {
        var response = await _httpClient.GetAsync("/api/tags");
        response.EnsureSuccessStatusCode();
        var tags = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>();
        
        // Prioritizza il modello suggerito "Phi-3", se no prendi il primo
        var phiModel = tags?.Models.FirstOrDefault(m => m.Name.Contains("Phi-3", StringComparison.OrdinalIgnoreCase));
        var first = phiModel ?? tags?.Models.FirstOrDefault();
        
        if (first == null) throw new Exception("Nessun modello trovato in Ollama");
        return first.Name;
    }

    public void Initialize()
    {
        // Nessuna inizializzazione pesante in memoria necessaria per Ollama
    }

    public async Task<string> SummarizeTextAsync(string text)
    {
        string modelName = await GetFirstModelNameAsync();

        // Approx 1500 tokens (around 6000 chars)
        int maxCharsPerChunk = 6000; 
        var chunks = ChunkText(text, maxCharsPerChunk);
        
        StringBuilder fullSummary = new StringBuilder();

        foreach (var chunk in chunks)
        {
            var prompt = $"Sei un assistente allo studio. Riassumi i seguenti appunti estraendo in modo chiaro e discorsivo i concetti chiave. Cerca di essere conciso ma non perdere informazioni vitali.\n\nTesto da riassumere:\n{chunk}";
            
            var request = new OllamaGenerateRequest
            {
                Model = modelName,
                Prompt = prompt,
                Stream = false
            };

            var response = await _httpClient.PostAsJsonAsync("/api/generate", request);
            response.EnsureSuccessStatusCode();

            var generateResponse = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>();
            if (generateResponse != null && !string.IsNullOrWhiteSpace(generateResponse.Response))
            {
                fullSummary.AppendLine(generateResponse.Response.Trim());
                fullSummary.AppendLine();
            }
        }

        return fullSummary.ToString().Trim();
    }

    private List<string> ChunkText(string text, int chunkSize)
    {
        var chunks = new List<string>();
        for (int i = 0; i < text.Length; i += chunkSize)
        {
            if (i + chunkSize > text.Length)
                chunks.Add(text.Substring(i));
            else
                chunks.Add(text.Substring(i, chunkSize));
        }
        return chunks;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
