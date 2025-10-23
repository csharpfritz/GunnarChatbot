namespace Chatbot.Data;

/// <summary>
/// Represents a product category or collection with associated products and metadata
/// </summary>
public class CategoryData
{
    /// <summary>
    /// Category name (e.g., "Gaming Glasses", "FPS Gaming")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category description and overview
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Parent category (null for top-level categories)
    /// </summary>
    public string? ParentCategory { get; set; }

    /// <summary>
    /// List of products in this category
    /// </summary>
    public List<string> ProductSKUs { get; set; } = new();

    /// <summary>
    /// Key benefits specific to this category
    /// </summary>
    public List<string> KeyBenefits { get; set; } = new();

    /// <summary>
    /// Target audience for this category
    /// </summary>
    public List<string> TargetAudience { get; set; } = new();

    /// <summary>
    /// Category-specific features
    /// </summary>
    public List<string> Features { get; set; } = new();

    /// <summary>
    /// Recommended lens types for this category
    /// </summary>
    public List<string> RecommendedLensTypes { get; set; } = new();

    /// <summary>
    /// Category image URL
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Source URL where the category data was crawled from
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Date when the category was last updated
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}