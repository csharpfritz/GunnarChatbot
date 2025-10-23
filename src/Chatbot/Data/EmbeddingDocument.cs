namespace Chatbot.Data;

/// <summary>
/// Represents a document that will be stored in the vector embedding database
/// </summary>
public class EmbeddingDocument
{
    /// <summary>
    /// Unique identifier for the document
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Type of document (product, category, use_case, technology, lens_technology)
    /// </summary>
    public EmbeddingDocumentType Type { get; set; }

    /// <summary>
    /// Text content to be embedded and searched
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Structured metadata associated with the document
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Generated embedding vector (populated by embedding service)
    /// </summary>
    public float[]? EmbeddingVector { get; set; }

    /// <summary>
    /// Date when the document was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when the document was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Source data identifier (ProductData SKU, CategoryData name, etc.)
    /// </summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// Quality score for the embedding (0.0 to 1.0)
    /// </summary>
    public double QualityScore { get; set; } = 1.0;

    /// <summary>
    /// Whether this document is active and should be included in searches
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Language of the content (default: English)
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Content chunk index (for documents split into multiple chunks)
    /// </summary>
    public int ChunkIndex { get; set; } = 0;

    /// <summary>
    /// Total number of chunks for the source content
    /// </summary>
    public int TotalChunks { get; set; } = 1;
}

/// <summary>
/// Types of embedding documents that can be created
/// </summary>
public enum EmbeddingDocumentType
{
    /// <summary>
    /// Individual product information
    /// </summary>
    Product,

    /// <summary>
    /// Category or collection overview
    /// </summary>
    Category,

    /// <summary>
    /// Use case or scenario-based information
    /// </summary>
    UseCase,

    /// <summary>
    /// Technology or feature explanation
    /// </summary>
    Technology,

    /// <summary>
    /// Lens-specific technology and information
    /// </summary>
    LensTechnology,

    /// <summary>
    /// General content (blogs, guides, FAQ)
    /// </summary>
    Content,

    /// <summary>
    /// Cross-reference or relationship information
    /// </summary>
    CrossReference
}