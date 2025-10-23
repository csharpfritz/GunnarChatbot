namespace Chatbot.Data;

/// <summary>
/// Represents a Gunnar product with all its specifications, features, and lens options
/// </summary>
public class ProductData
{
    /// <summary>
    /// Product name (e.g., "Intercept", "Siege")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Stock Keeping Unit - unique product identifier
    /// </summary>
    public string SKU { get; set; } = string.Empty;

    /// <summary>
    /// Detailed product description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// List of key product features
    /// </summary>
    public List<string> Features { get; set; } = new();

    /// <summary>
    /// Default lens type that comes with the product
    /// </summary>
    public string DefaultLensType { get; set; } = string.Empty;

    /// <summary>
    /// All available lens options for this product
    /// </summary>
    public List<LensOption> SupportedLenses { get; set; } = new();

    /// <summary>
    /// Frame type/style (e.g., "Full Frame", "Semi-Rimless")
    /// </summary>
    public string FrameType { get; set; } = string.Empty;

    /// <summary>
    /// Primary frame color
    /// </summary>
    public string FrameColor { get; set; } = string.Empty;

    /// <summary>
    /// Base price for the product with default lens
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Product image URLs
    /// </summary>
    public List<string> Images { get; set; } = new();

    /// <summary>
    /// Primary category (e.g., "Gaming Glasses", "Computer Glasses")
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Collection or brand collaboration (e.g., "Razer", "Blizzard")
    /// </summary>
    public string Collection { get; set; } = string.Empty;

    /// <summary>
    /// Technical specifications and measurements
    /// </summary>
    public Dictionary<string, string> Specifications { get; set; } = new();

    /// <summary>
    /// Fit guide information (sizing, face shape recommendations)
    /// </summary>
    public string FitGuide { get; set; } = string.Empty;

    /// <summary>
    /// Searchable tags for categorization and filtering
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Product availability status
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Date when the product was last updated
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Source URL where the product data was crawled from
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;
}