using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLama;
using LLama.Common;

namespace FocusDesk.Ubuntu.Services;

public class LocalLlmSummarizer : IDisposable
{
    private static readonly string ModelsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FocusDesk",
        "Models");

    private LLamaWeights? _weights;
    private LLamaContext? _context;
    private InteractiveExecutor? _executor;
    
    private const int MaxContextTokens = 2048; // Bilanciamo prestazioni e contesto
    
    public bool IsModelAvailable()
    {
        if (!Directory.Exists(ModelsFolder))
            return false;
            
        return Directory.GetFiles(ModelsFolder, "*.gguf").Length > 0;
    }

    public string GetModelPath()
    {
        if (!Directory.Exists(ModelsFolder))
            Directory.CreateDirectory(ModelsFolder);

        var models = Directory.GetFiles(ModelsFolder, "*.gguf");
        return models.Length > 0 ? models[0] : string.Empty;
    }

    public void Initialize()
    {
        if (_weights != null) return; // Già inizializzato
        
        string modelPath = GetModelPath();
        if (string.IsNullOrEmpty(modelPath))
            throw new FileNotFoundException("Nessun modello .gguf trovato in " + ModelsFolder);

        var parameters = new ModelParams(modelPath)
        {
            ContextSize = MaxContextTokens,
            GpuLayerCount = 0 // Usiamo CPU fallback
        };

        _weights = LLamaWeights.LoadFromFile(parameters);
        _context = _weights.CreateContext(parameters);
        _executor = new InteractiveExecutor(_context);
    }

    public async Task<string> SummarizeTextAsync(string text)
    {
        if (_executor == null)
            Initialize();

        // Semplice suddivisione in chunk se il testo è troppo lungo.
        // Approssimiamo 1 token = 4 caratteri
        int maxCharsPerChunk = (MaxContextTokens - 500) * 4; 
        var chunks = ChunkText(text, maxCharsPerChunk);
        
        StringBuilder fullSummary = new StringBuilder();

        foreach (var chunk in chunks)
        {
            var prompt = $"<|system|>\nSei un assistente allo studio. Riassumi i seguenti appunti estraendo in modo chiaro e discorsivo i concetti chiave. Cerca di essere conciso ma non perdere informazioni vitali.<|end|>\n<|user|>\nRiassumi questo testo:\n\n{chunk}<|end|>\n<|assistant|>\n";
            
            var inferenceParams = new InferenceParams()
            {
                MaxTokens = 500,
                Temperature = 0.3f,
                AntiPrompts = new List<string> { "<|end|>", "<|user|>" }
            };

            StringBuilder chunkSummary = new StringBuilder();
            
            await foreach (var token in _executor!.InferAsync(prompt, inferenceParams))
            {
                chunkSummary.Append(token);
            }
            
            fullSummary.AppendLine(chunkSummary.ToString().Trim());
            fullSummary.AppendLine();
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
        _context?.Dispose();
        _weights?.Dispose();
    }
}
