namespace Chatbot.Data;

/// <summary>
/// Represents a specific lens option available for a Gunnar product
/// </summary>
public class LensOption
{
    /// <summary>
    /// Type of lens (Amber, Clear, Prescription, Sunglass, Dark Amber, Photochromic)
    /// </summary>
    public string LensType { get; set; } = string.Empty;

    /// <summary>
    /// Percentage of blue light protection provided (e.g., "65%", "35%", "98%")
    /// </summary>
    public string BlueLightProtection { get; set; } = string.Empty;

    /// <summary>
    /// Additional cost for this lens option (0 for standard options)
    /// </summary>
    public decimal PriceModifier { get; set; }

    /// <summary>
    /// Whether this lens option is currently available
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Detailed description of the lens type and its characteristics
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Key benefits of this lens type
    /// </summary>
    public List<string> Benefits { get; set; } = new();

    /// <summary>
    /// Recommended use cases for this lens type
    /// </summary>
    public List<string> RecommendedUses { get; set; } = new();

    /// <summary>
    /// Color enhancement characteristics
    /// </summary>
    public string ColorEnhancement { get; set; } = string.Empty;

    /// <summary>
    /// UV protection level (for sunglass lenses)
    /// </summary>
    public string UVProtection { get; set; } = string.Empty;

    /// <summary>
    /// Manufacturing time for custom lenses (prescription)
    /// </summary>
    public string ManufacturingTime { get; set; } = string.Empty;

    /// <summary>
    /// Specific tint options (for sunglass lenses)
    /// </summary>
    public List<string> TintOptions { get; set; } = new();
}