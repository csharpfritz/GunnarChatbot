namespace Chatbot.Data;

/// <summary>
/// Represents content data from blogs, support pages, and guides
/// </summary>
public class ContentData
{
    /// <summary>
    /// Content title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Content body text
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Type of content (Blog, FAQ, Guide, Support)
    /// </summary>
    public ContentType Type { get; set; }

    /// <summary>
    /// Content category or topic
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Tags for content categorization
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Related product SKUs mentioned in the content
    /// </summary>
    public List<string> RelatedProducts { get; set; } = new();

    /// <summary>
    /// Key topics covered in the content
    /// </summary>
    public List<string> KeyTopics { get; set; } = new();

    /// <summary>
    /// Content publication date
    /// </summary>
    public DateTime PublishDate { get; set; }

    /// <summary>
    /// Last modified date
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Source URL of the content
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Content author (if available)
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Content summary/excerpt
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Whether this content is currently active/published
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Types of content that can be crawled and indexed
/// </summary>
public enum ContentType
{
    /// <summary>
    /// Blog posts and articles
    /// </summary>
    Blog,

    /// <summary>
    /// Frequently asked questions
    /// </summary>
    FAQ,

    /// <summary>
    /// User guides and tutorials
    /// </summary>
    Guide,

    /// <summary>
    /// Support documentation
    /// </summary>
    Support,

    /// <summary>
    /// Product fitting guides
    /// </summary>
    FitGuide,

    /// <summary>
    /// Lens and technology guides
    /// </summary>
    TechnologyGuide,

    /// <summary>
    /// Care and maintenance instructions
    /// </summary>
    CareInstructions
}