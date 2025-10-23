namespace Chatbot.Data;

/// <summary>
/// Comprehensive information about Gunnar lens types and their characteristics
/// </summary>
public static class LensTypes
{
    /// <summary>
    /// All available Gunnar lens types
    /// </summary>
    public static readonly Dictionary<string, LensTypeInfo> All = new()
    {
        ["Amber"] = new LensTypeInfo
        {
            Name = "Amber/Crystalline",
            BlueLightProtection = "65%",
            ColorEnhancement = "High contrast, warm tint",
            BestFor = new[] { "Gaming", "Low light environments", "Evening computer use" },
            Characteristics = new[] { "Enhanced contrast", "Reduced eye strain", "Improved sleep patterns" },
            IsGamingOptimized = true,
            HasUVProtection = false,
            IsPrescriptionAvailable = true
        },
        
        ["Clear"] = new LensTypeInfo
        {
            Name = "Clear/Liquet",
            BlueLightProtection = "35%",
            ColorEnhancement = "Natural color accuracy",
            BestFor = new[] { "Office work", "Professional environments", "All-day computer use" },
            Characteristics = new[] { "Minimal color distortion", "Subtle protection", "Professional appearance" },
            IsGamingOptimized = false,
            HasUVProtection = false,
            IsPrescriptionAvailable = true
        },
        
        ["Dark Amber"] = new LensTypeInfo
        {
            Name = "Dark Amber/Umber",
            BlueLightProtection = "80%",
            ColorEnhancement = "Maximum contrast enhancement",
            BestFor = new[] { "Severe light sensitivity", "Post-surgery recovery", "Maximum protection" },
            Characteristics = new[] { "Highest protection level", "Significant color shift", "Medical grade filtering" },
            IsGamingOptimized = true,
            HasUVProtection = false,
            IsPrescriptionAvailable = true
        },
        
        ["Prescription"] = new LensTypeInfo
        {
            Name = "Prescription (Rx)",
            BlueLightProtection = "Varies by base lens",
            ColorEnhancement = "Based on selected lens type",
            BestFor = new[] { "Vision correction needed", "Custom prescriptions", "Progressive lenses" },
            Characteristics = new[] { "Custom vision correction", "Available in multiple lens types", "Single vision or progressive" },
            IsGamingOptimized = null, // Depends on base lens type
            HasUVProtection = null,   // Depends on base lens type
            IsPrescriptionAvailable = true,
            AdditionalInfo = new Dictionary<string, object>
            {
                ["prescription_types"] = new[] { "Single Vision", "Progressive", "Bifocal", "Reading" },
                ["available_lens_bases"] = new[] { "Clear", "Amber", "Sunglass tints" },
                ["typical_cost_addition"] = "$150-$300"
            }
        },
        
        ["Sunglass"] = new LensTypeInfo
        {
            Name = "Sunglass Tints",
            BlueLightProtection = "85-98%",
            ColorEnhancement = "Various tint options",
            BestFor = new[] { "Outdoor use", "Bright environments", "UV protection" },
            Characteristics = new[] { "UV protection", "Glare reduction", "Multiple tint options" },
            IsGamingOptimized = false,
            HasUVProtection = true,
            IsPrescriptionAvailable = true,
            AdditionalInfo = new Dictionary<string, object>
            {
                ["tint_options"] = new[] { "Grey", "Brown", "Green", "Gradient" }
            }
        },
        
        ["Photochromic"] = new LensTypeInfo
        {
            Name = "Photochromic/Transitions",
            BlueLightProtection = "Variable (35-85%)",
            ColorEnhancement = "Adaptive based on lighting",
            BestFor = new[] { "Variable lighting", "Indoor/outdoor transitions", "All-day wear" },
            Characteristics = new[] { "Automatically adjusts", "Light-responsive", "Convenience factor" },
            IsGamingOptimized = null, // Depends on light conditions
            HasUVProtection = true,
            IsPrescriptionAvailable = true
        }
    };

    /// <summary>
    /// Get lens type information by name
    /// </summary>
    /// <param name="lensTypeName">Name of the lens type</param>
    /// <returns>Lens type information or null if not found</returns>
    public static LensTypeInfo? GetLensType(string lensTypeName)
    {
        return All.TryGetValue(lensTypeName, out var lensType) ? lensType : null;
    }

    /// <summary>
    /// Get all lens types optimized for gaming
    /// </summary>
    /// <returns>List of gaming-optimized lens types</returns>
    public static List<string> GetGamingOptimizedLenses()
    {
        return All.Where(kvp => kvp.Value.IsGamingOptimized == true)
                  .Select(kvp => kvp.Key)
                  .ToList();
    }

    /// <summary>
    /// Get all lens types with UV protection
    /// </summary>
    /// <returns>List of UV-protective lens types</returns>
    public static List<string> GetUVProtectiveLenses()
    {
        return All.Where(kvp => kvp.Value.HasUVProtection == true)
                  .Select(kvp => kvp.Key)
                  .ToList();
    }

    /// <summary>
    /// Get all lens types available as prescription
    /// </summary>
    /// <returns>List of prescription-available lens types</returns>
    public static List<string> GetPrescriptionAvailableLenses()
    {
        return All.Where(kvp => kvp.Value.IsPrescriptionAvailable)
                  .Select(kvp => kvp.Key)
                  .ToList();
    }
}

/// <summary>
/// Detailed information about a specific lens type
/// </summary>
public class LensTypeInfo
{
    /// <summary>
    /// Display name of the lens type
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Blue light protection percentage or range
    /// </summary>
    public string BlueLightProtection { get; set; } = string.Empty;

    /// <summary>
    /// Color enhancement characteristics
    /// </summary>
    public string ColorEnhancement { get; set; } = string.Empty;

    /// <summary>
    /// Best use cases for this lens type
    /// </summary>
    public string[] BestFor { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Key characteristics of the lens type
    /// </summary>
    public string[] Characteristics { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Whether this lens type is optimized for gaming (null = depends on conditions)
    /// </summary>
    public bool? IsGamingOptimized { get; set; }

    /// <summary>
    /// Whether this lens type provides UV protection
    /// </summary>
    public bool? HasUVProtection { get; set; }

    /// <summary>
    /// Whether this lens type is available as a prescription option
    /// </summary>
    public bool IsPrescriptionAvailable { get; set; }

    /// <summary>
    /// Additional lens-specific information
    /// </summary>
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}