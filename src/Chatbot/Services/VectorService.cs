using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Chatbot.Services;

public class VectorService
{
    private readonly QdrantClient _qdrantClient;
    private readonly EmbeddingService _embeddingService;
    private readonly ILogger<VectorService> _logger;
    private const string CollectionName = "gunnar-products";

    public VectorService(QdrantClient qdrantClient, EmbeddingService embeddingService, ILogger<VectorService> logger)
    {
        _qdrantClient = qdrantClient;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    /// <summary>
    /// Initialize the Qdrant collection for Gunnar products
    /// </summary>
    public async Task InitializeCollectionAsync()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("🔍 Initializing Qdrant collection: {CollectionName}", CollectionName);
            
            // Check if collection exists
            _logger.LogDebug("📋 Checking if collection exists...");
            var collections = await _qdrantClient.ListCollectionsAsync();
            
            if (collections.Any(c => c == CollectionName))
            {
                stopwatch.Stop();
                _logger.LogInformation("✅ Collection '{CollectionName}' already exists (checked in {Duration}ms)", 
                    CollectionName, stopwatch.ElapsedMilliseconds);
                return; // Collection already exists
            }

            // Create collection with appropriate vector configuration
            _logger.LogInformation("🚀 Creating new collection '{CollectionName}' with 768-dimensional vectors and cosine distance", CollectionName);
            
            var createStopwatch = System.Diagnostics.Stopwatch.StartNew();
            await _qdrantClient.CreateCollectionAsync(CollectionName, new VectorParams
            {
                Size = 768, // nomic-embed-text produces 768-dimensional vectors
                Distance = Distance.Cosine
            });
            createStopwatch.Stop();
            
            stopwatch.Stop();
            
            _logger.LogInformation("🎉 Successfully created collection '{CollectionName}' in {TotalDuration}ms [Creation: {CreationDuration}ms]",
                CollectionName, stopwatch.ElapsedMilliseconds, createStopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "💥 Failed to initialize Qdrant collection '{CollectionName}' after {Duration}ms: {ErrorMessage}",
                CollectionName, stopwatch.ElapsedMilliseconds, ex.Message);
            throw new InvalidOperationException($"Failed to initialize Qdrant collection: {CollectionName}", ex);
        }
    }

    /// <summary>
    /// Add a product to the vector database
    /// </summary>
    /// <param name="productId">Unique identifier for the product (SKU)</param>
    /// <param name="productText">Text description of the product (name, description, features, etc.)</param>
    /// <param name="metadata">Additional metadata about the product</param>
    public async Task AddProductAsync(string productId, string productText, Dictionary<string, Value>? metadata = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("🔄 Starting vector indexing for product ID: {ProductId}", productId);

            // Generate a deterministic UUID from the product ID (SKU)
            var pointUuid = GenerateUuidFromString(productId);
            _logger.LogDebug("🆔 Generated UUID {Uuid} from product ID: {ProductId}", pointUuid, productId);

            // Generate embeddings for the product text
            _logger.LogDebug("🧠 Generating embeddings for product text (length: {TextLength} chars)", productText.Length);
            var embeddingStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(productText);
            embeddingStopwatch.Stop();
            
            _logger.LogDebug("✅ Generated {EmbeddingCount} embeddings in {Duration}ms", embeddings.Length, embeddingStopwatch.ElapsedMilliseconds);
            
            // Create the payload
            var payload = metadata ?? new Dictionary<string, Value>();
            payload["product_text"] = productText;
            payload["product_id"] = productId; // Store the original SKU in metadata
            payload["indexed_at"] = DateTime.UtcNow.ToString("O");

            _logger.LogDebug("📦 Created payload with {MetadataCount} metadata fields", payload.Count);

            // Create the point to insert
            var point = new PointStruct
            {
                Id = new PointId { Uuid = pointUuid },
                Vectors = embeddings,
                Payload = { payload }
            };

            // Insert the point into Qdrant
            _logger.LogDebug("💾 Upserting point to Qdrant collection: {CollectionName}", CollectionName);
            var upsertStopwatch = System.Diagnostics.Stopwatch.StartNew();
            await _qdrantClient.UpsertAsync(CollectionName, new[] { point });
            upsertStopwatch.Stop();

            stopwatch.Stop();
            
            _logger.LogInformation("🎉 Successfully indexed product '{ProductId}' with UUID {Uuid} in {TotalDuration}ms [Embedding: {EmbeddingDuration}ms, Upsert: {UpsertDuration}ms]",
                productId, pointUuid, stopwatch.ElapsedMilliseconds, embeddingStopwatch.ElapsedMilliseconds, upsertStopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "💥 Failed to add product {ProductId} to vector database after {Duration}ms: {ErrorMessage}",
                productId, stopwatch.ElapsedMilliseconds, ex.Message);
            throw new InvalidOperationException($"Failed to add product {productId} to vector database", ex);
        }
    }

    /// <summary>
    /// Search for similar products based on a query
    /// </summary>
    /// <param name="query">User's search query</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="scoreThreshold">Minimum similarity score (0.0 to 1.0)</param>
    /// <returns>List of similar products with their scores</returns>
    public async Task<List<ScoredPoint>> SearchSimilarProductsAsync(string query, ulong limit = 5, float scoreThreshold = 0.7f)
    {
        try
        {
            // Generate embeddings for the search query
            var queryEmbeddings = await _embeddingService.GenerateEmbeddingsAsync(query);

            // Search for similar vectors
            var searchResult = await _qdrantClient.SearchAsync(CollectionName, queryEmbeddings, limit: limit, scoreThreshold: scoreThreshold);

            return searchResult.ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to search for products with query: {query}", ex);
        }
    }

    /// <summary>
    /// Get collection information
    /// </summary>
    public async Task<CollectionInfo> GetCollectionInfoAsync()
    {
        return await _qdrantClient.GetCollectionInfoAsync(CollectionName);
    }

    /// <summary>
    /// Clear all products from the collection
    /// </summary>
    public async Task ClearCollectionAsync()
    {
        await _qdrantClient.DeleteCollectionAsync(CollectionName);
        await InitializeCollectionAsync();
    }

    /// <summary>
    /// Generate a deterministic UUID from a string (like SKU) - exposed for testing and logging
    /// This ensures the same SKU always generates the same UUID
    /// </summary>
    /// <param name="input">Input string (typically the product SKU)</param>
    /// <returns>Deterministic UUID as string</returns>
    public static string GenerateUuidFromString(string input)
    {
        // Use SHA-256 to create a deterministic hash from the input string
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        
        // Take the first 16 bytes of the hash to create a UUID
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        
        // Create a GUID from the bytes and return as string
        var guid = new Guid(guidBytes);
        return guid.ToString();
    }
}