using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Chatbot.Services;

public class VectorService
{
    private readonly QdrantClient _qdrantClient;
    private readonly EmbeddingService _embeddingService;
    private const string CollectionName = "gunnar-products";

    public VectorService(QdrantClient qdrantClient, EmbeddingService embeddingService)
    {
        _qdrantClient = qdrantClient;
        _embeddingService = embeddingService;
    }

    /// <summary>
    /// Initialize the Qdrant collection for Gunnar products
    /// </summary>
    public async Task InitializeCollectionAsync()
    {
        try
        {
            // Check if collection exists
            var collections = await _qdrantClient.ListCollectionsAsync();
            if (collections.Any(c => c == CollectionName))
            {
                return; // Collection already exists
            }

            // Create collection with appropriate vector configuration
            await _qdrantClient.CreateCollectionAsync(CollectionName, new VectorParams
            {
                Size = 768, // nomic-embed-text produces 768-dimensional vectors
                Distance = Distance.Cosine
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to initialize Qdrant collection: {CollectionName}", ex);
        }
    }

    /// <summary>
    /// Add a product to the vector database
    /// </summary>
    /// <param name="productId">Unique identifier for the product</param>
    /// <param name="productText">Text description of the product (name, description, features, etc.)</param>
    /// <param name="metadata">Additional metadata about the product</param>
    public async Task AddProductAsync(string productId, string productText, Dictionary<string, Value>? metadata = null)
    {
        try
        {
            // Generate embeddings for the product text
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(productText);
            
            // Create the payload
            var payload = metadata ?? new Dictionary<string, Value>();
            payload["product_text"] = productText;
            payload["indexed_at"] = DateTime.UtcNow.ToString("O");

            // Create the point to insert
            var point = new PointStruct
            {
                Id = new PointId { Uuid = productId },
                Vectors = embeddings,
                Payload = { payload }
            };

            // Insert the point into Qdrant
            await _qdrantClient.UpsertAsync(CollectionName, new[] { point });
        }
        catch (Exception ex)
        {
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
}