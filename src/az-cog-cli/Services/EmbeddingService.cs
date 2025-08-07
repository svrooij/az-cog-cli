using OpenAI;
using OpenAI.Embeddings;
using System.Security.Cryptography;
using System.Text;
using System.ClientModel;
using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;

namespace AzCogCli.Services;

/// <summary>
/// Represents the result of an embedding generation
/// Because structure makes everything better! 📦✨
/// </summary>
public class EmbeddingResult
{
    /// <summary>
    /// SHA256 hash of the text (including model) for caching
    /// </summary>
    public string Hash { get; init; } = string.Empty;
    
    /// <summary>
    /// The generated embedding vector
    /// </summary>
    public ReadOnlyMemory<float> Embedding { get; init; }
    
    /// <summary>
    /// The model used to generate this embedding
    /// </summary>
    public string Model { get; init; } = string.Empty;
}

/// <summary>
/// Service for generating text embeddings using OpenAI
/// Because turning text into numbers is apparently the future! 🔮✨
/// </summary>
public class EmbeddingService
{
    private readonly AzureOpenAIClient _openaiClient;
    private readonly string _model;
    private readonly Dictionary<string, EmbeddingResult> _cache;
    private readonly string _cacheFilePath;
    
    // Default to the most popular kid on the block 🎯
    public const string DefaultModel = "text-embedding-3-small"; // 1536 dimensions of pure joy! 🎪
    
    public EmbeddingService(string apiKey, string? baseUrl = null, string model = DefaultModel, string? cacheDirectory = null)
    {
        _model = model;
        _cache = new Dictionary<string, EmbeddingResult>();
        
        // Set up cache file path (defaults to temp directory if not specified)
        var cacheDir = cacheDirectory ?? Path.GetTempPath();
        var sanitizedModel = model.Replace('/', '_').Replace('\\', '_'); // Make filename safe
        _cacheFilePath = Path.Combine(cacheDir, $"embeddings-{sanitizedModel}.json");
        
        // Create client with optional custom endpoint (for the rebels using Azure OpenAI! 😎)
        var options = new OpenAIClientOptions()
        {
            Endpoint = new Uri(baseUrl!)
        };
        // if (!string.IsNullOrEmpty(baseUrl))
        // {
        //     options.Endpoint = new Uri(baseUrl);
        // }
        _openaiClient = new AzureOpenAIClient(new Uri(baseUrl!), new ApiKeyCredential(apiKey));
        // _embeddingClient = new EmbeddingClient(model, new AzureKeyCredential(apiKey), options);
        
        // Load existing cache on startup 📂
        // Note: We don't await this in constructor to avoid blocking
        //Task.Run(async () => await LoadCacheAsync());
    }
    
    /// <summary>
    /// Generates embeddings for the given texts
    /// Returns a list of EmbeddingResult objects with hash and embeddings
    /// Uses cache-first approach - because why pay OpenAI twice for the same thing? �✨
    /// </summary>
    public async Task<List<EmbeddingResult>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts, 
        CancellationToken cancellationToken = default)
    {
        var textList = texts.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        if (!textList.Any())
        {
            return new List<EmbeddingResult>(); // Empty handed but not empty hearted! 
        }
        
        Console.WriteLine($"🧠 Processing {textList.Count} text(s) for embeddings using {_model}...");
        
        var results = new List<EmbeddingResult>();
        var textsNeedingGeneration = new List<(string text, int originalIndex)>();
        
        // 🔍 Check cache first (because cache is love, cache is life!)
        for (int i = 0; i < textList.Count; i++)
        {
            var text = textList[i];
            var hash = ComputeTextHash(text);
            
            if (_cache.TryGetValue(hash, out var cachedResult))
            {
                results.Add(cachedResult);
                Console.WriteLine($"💾 Cache hit for text {i + 1}/{textList.Count}");
            }
            else
            {
                textsNeedingGeneration.Add((text, i));
                results.Add(null!); // Placeholder - we'll fill this in later
            }
        }
        
        // 🚀 Generate embeddings for cache misses
        if (textsNeedingGeneration.Any())
        {
            Console.WriteLine($"🔄 Generating {textsNeedingGeneration.Count} new embedding(s) via OpenAI...");
            
            try
            {
                //var textsToGenerate = textsNeedingGeneration.Select(x => x.text).ToList();
                var embeddingClient = _openaiClient.GetEmbeddingClient(_model);
                
                // 📦 Store new embeddings in cache and results
                for (int i = 0; i < textsNeedingGeneration.Count; i++)
                {
                    var (text, originalIndex) = textsNeedingGeneration[i];
                    var embedding = await embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
                    var hash = ComputeTextHash(text);
                    
                    var embeddingResult = new EmbeddingResult
                    {
                        Hash = hash,
                        Embedding = embedding.Value.ToFloats(),
                        Model = _model
                    };
                    
                    // Add to cache
                    _cache[hash] = embeddingResult;
                    
                    // Fill in the result at the correct position
                    results[originalIndex] = embeddingResult;
                }
                
                // 💾 Auto-save cache after new entries
                await SaveCacheAsync(cancellationToken);
                
                Console.WriteLine($"✅ Generated {textsNeedingGeneration.Count} new embedding(s) and saved to cache!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"😵 OpenAI embedding generation failed: {ex.Message}");
                Console.WriteLine("💡 Check your API key, quota, and internet connection!");
                throw; // Re-throw because we're not in the business of hiding problems! 🕵️‍♂️
            }
        }
        else
        {
            Console.WriteLine($"💾 All {textList.Count} embedding(s) found in cache - no OpenAI calls needed! 🎉");
        }
        
        return results;
    }
    
    /// <summary>
    /// Generates embedding for a single text
    /// For those times when you just need one vector! 🎯
    /// </summary>
    public async Task<EmbeddingResult> GenerateEmbeddingAsync(
        string text, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be null or empty! Even AI needs something to work with! 🤷‍♂️", nameof(text));
        }
        
        var results = await GenerateEmbeddingsAsync(new[] { text }, cancellationToken);
        return results[0]; // First (and only) result! 🎯
    }
    
    /// <summary>
    /// Computes a hash for the given text to use as cache key
    /// SHA256 because we're serious about our hashing! 🔐
    /// </summary>
    public string ComputeTextHash(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
            
        // Include model in hash to avoid cache conflicts when switching models
        // Because model A's embedding != model B's embedding (shocking, I know! 😱)
        var input = $"{_model}|{text}";
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        
        return Convert.ToHexString(hash).ToLowerInvariant(); // Lowercase because we're civilized! 🎩
    }
    
    /// <summary>
    /// Gets the model name being used
    /// In case you forgot what you're using! 🤔
    /// </summary>
    public string Model => _model;
    
    /// <summary>
    /// Gets the expected embedding dimensions for the current model
    /// Because size matters (in vector space)! 📏
    /// </summary>
    public int GetEmbeddingDimensions()
    {
        return _model switch
        {
            "text-embedding-3-small" => 1536,  // Our default champion! 🏆
            "text-embedding-3-large" => 3072,  // For when you need MORE dimensions! 📈
            "text-embedding-ada-002" => 1536,  // The classic choice! 🏛️
            _ => 1536 // When in doubt, go with 1536! 🎲
        };
    }
        
    /// <summary>
    /// Loads the embedding cache from disk
    /// Because persistence is key (pun intended)! 🔑
    /// </summary>
    public async Task LoadCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
            {
                Console.WriteLine($"📄 No cache file found at {_cacheFilePath} - starting with empty cache");
                return;
            }
            
            var jsonContent = await File.ReadAllTextAsync(_cacheFilePath, cancellationToken);
            var cacheData = JsonSerializer.Deserialize<Dictionary<string, CacheEntry>>(jsonContent);
            
            if (cacheData != null)
            {
                _cache.Clear();
                foreach (var kvp in cacheData)
                {
                    // Only load entries that match our current model
                    if (kvp.Value.Model == _model)
                    {
                        _cache[kvp.Key] = new EmbeddingResult
                        {
                            Hash = kvp.Key,
                            Embedding = new ReadOnlyMemory<float>(kvp.Value.Embedding),
                            Model = kvp.Value.Model
                        };
                    }
                }
                
                Console.WriteLine($"💾 Loaded {_cache.Count} embedding(s) from cache for model {_model}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Failed to load cache: {ex.Message}");
            Console.WriteLine("🆕 Starting with empty cache...");
            _cache.Clear();
        }
    }
    
    /// <summary>
    /// Saves the embedding cache to disk
    /// Auto-save is love, auto-save is life! 💾✨
    /// </summary>
    public async Task SaveCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Convert to serializable format
            var cacheData = _cache.ToDictionary(
                kvp => kvp.Key,
                kvp => new CacheEntry
                {
                    Embedding = kvp.Value.Embedding.ToArray(),
                    Model = kvp.Value.Model,
                    CreatedAt = DateTime.UtcNow
                }
            );
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_cacheFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var jsonContent = JsonSerializer.Serialize(cacheData, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(_cacheFilePath, jsonContent, cancellationToken);
            Console.WriteLine($"💾 Saved {_cache.Count} embedding(s) to cache file");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Failed to save cache: {ex.Message}");
            // Don't throw - cache save failure shouldn't break the main flow
        }
    }
    
    /// <summary>
    /// Clears the in-memory cache and optionally deletes the cache file
    /// For those times when you need a fresh start! 🧹
    /// </summary>
    public async Task ClearCacheAsync(bool deleteFile = false)
    {
        _cache.Clear();
        
        if (deleteFile && File.Exists(_cacheFilePath))
        {
            try
            {
                File.Delete(_cacheFilePath);
                Console.WriteLine($"🗑️  Deleted cache file: {_cacheFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Failed to delete cache file: {ex.Message}");
            }
        }
        
        Console.WriteLine("🧹 Cache cleared!");
    }
}

/// <summary>
/// Internal class for JSON serialization of cache entries
/// Because we need to be able to save our precious embeddings! 💎
/// </summary>
internal class CacheEntry
{
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public string Model { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
