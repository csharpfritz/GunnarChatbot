using System.ComponentModel.DataAnnotations;

namespace Chatbot.Data;

/// <summary>
/// Validation helper for product data integrity
/// </summary>
public static class DataValidator
{
    /// <summary>
    /// Validates a ProductData object for completeness and consistency
    /// </summary>
    /// <param name="product">Product to validate</param>
    /// <returns>Validation result with any errors</returns>
    public static ValidationResult ValidateProduct(ProductData product)
    {
        var result = new ValidationResult();
        
        // Required fields validation
        if (string.IsNullOrWhiteSpace(product.Name))
            result.AddError("Product name is required");
            
        if (string.IsNullOrWhiteSpace(product.SKU))
            result.AddError("Product SKU is required");
            
        if (string.IsNullOrWhiteSpace(product.Description))
            result.AddError("Product description is required");
            
        if (string.IsNullOrWhiteSpace(product.Category))
            result.AddError("Product category is required");
            
        // Price validation
        if (product.Price < 0)
            result.AddError("Product price cannot be negative");
            
        // Lens validation
        if (string.IsNullOrWhiteSpace(product.DefaultLensType))
            result.AddError("Default lens type is required");
            
        if (!product.SupportedLenses.Any())
            result.AddError("At least one supported lens option is required");
            
        // Validate each lens option
        foreach (var lens in product.SupportedLenses)
        {
            var lensValidation = ValidateLensOption(lens);
            if (!lensValidation.IsValid)
                result.AddErrors($"Lens '{lens.LensType}': ", lensValidation.Errors);
        }
        
        // Validate default lens type is in supported lenses
        if (!product.SupportedLenses.Any(l => l.LensType == product.DefaultLensType))
            result.AddError("Default lens type must be included in supported lenses");
            
        return result;
    }

    /// <summary>
    /// Validates a LensOption object
    /// </summary>
    /// <param name="lens">Lens option to validate</param>
    /// <returns>Validation result with any errors</returns>
    public static ValidationResult ValidateLensOption(LensOption lens)
    {
        var result = new ValidationResult();
        
        if (string.IsNullOrWhiteSpace(lens.LensType))
            result.AddError("Lens type is required");
            
        if (string.IsNullOrWhiteSpace(lens.BlueLightProtection))
            result.AddError("Blue light protection specification is required");
            
        if (lens.PriceModifier < 0)
            result.AddError("Price modifier cannot be negative");
            
        // Validate lens type exists in our predefined types
        if (!string.IsNullOrWhiteSpace(lens.LensType) && 
            LensTypes.GetLensType(lens.LensType) == null)
            result.AddWarning($"Lens type '{lens.LensType}' is not in predefined lens types");
            
        return result;
    }

    /// <summary>
    /// Validates a CategoryData object
    /// </summary>
    /// <param name="category">Category to validate</param>
    /// <returns>Validation result with any errors</returns>
    public static ValidationResult ValidateCategory(CategoryData category)
    {
        var result = new ValidationResult();
        
        if (string.IsNullOrWhiteSpace(category.Name))
            result.AddError("Category name is required");
            
        if (string.IsNullOrWhiteSpace(category.Description))
            result.AddError("Category description is required");
            
        return result;
    }

    /// <summary>
    /// Validates an EmbeddingDocument object
    /// </summary>
    /// <param name="document">Document to validate</param>
    /// <returns>Validation result with any errors</returns>
    public static ValidationResult ValidateEmbeddingDocument(EmbeddingDocument document)
    {
        var result = new ValidationResult();
        
        if (string.IsNullOrWhiteSpace(document.Id))
            result.AddError("Document ID is required");
            
        if (string.IsNullOrWhiteSpace(document.Content))
            result.AddError("Document content is required");
            
        if (string.IsNullOrWhiteSpace(document.SourceId))
            result.AddError("Source ID is required");
            
        if (document.QualityScore < 0 || document.QualityScore > 1)
            result.AddError("Quality score must be between 0 and 1");
            
        return result;
    }
}

/// <summary>
/// Represents the result of a data validation operation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; } = new();

    /// <summary>
    /// List of validation warnings
    /// </summary>
    public List<string> Warnings { get; } = new();

    /// <summary>
    /// Whether the validation passed (no errors)
    /// </summary>
    public bool IsValid => !Errors.Any();

    /// <summary>
    /// Total number of issues (errors + warnings)
    /// </summary>
    public int IssueCount => Errors.Count + Warnings.Count;

    /// <summary>
    /// Add an error to the validation result
    /// </summary>
    /// <param name="error">Error message</param>
    public void AddError(string error)
    {
        Errors.Add(error);
    }

    /// <summary>
    /// Add multiple errors with a prefix
    /// </summary>
    /// <param name="prefix">Prefix for each error</param>
    /// <param name="errors">List of errors</param>
    public void AddErrors(string prefix, IEnumerable<string> errors)
    {
        foreach (var error in errors)
        {
            Errors.Add(prefix + error);
        }
    }

    /// <summary>
    /// Add a warning to the validation result
    /// </summary>
    /// <param name="warning">Warning message</param>
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }

    /// <summary>
    /// Get a formatted summary of all validation issues
    /// </summary>
    /// <returns>Formatted validation summary</returns>
    public string GetSummary()
    {
        var summary = new List<string>();
        
        if (Errors.Any())
        {
            summary.Add($"Errors ({Errors.Count}):");
            summary.AddRange(Errors.Select(e => $"  - {e}"));
        }
        
        if (Warnings.Any())
        {
            summary.Add($"Warnings ({Warnings.Count}):");
            summary.AddRange(Warnings.Select(w => $"  - {w}"));
        }
        
        return summary.Any() ? string.Join(Environment.NewLine, summary) : "Validation passed";
    }
}