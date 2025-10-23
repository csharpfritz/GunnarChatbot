namespace Chatbot.Data;

/// <summary>
/// Represents the result of a web crawling operation
/// </summary>
public class CrawlResult
{
    /// <summary>
    /// Whether the crawl operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of products successfully crawled
    /// </summary>
    public int ProductsCrawled { get; set; }

    /// <summary>
    /// Number of categories successfully crawled
    /// </summary>
    public int CategoriesCrawled { get; set; }

    /// <summary>
    /// Number of content pages successfully crawled
    /// </summary>
    public int ContentPagesCrawled { get; set; }

    /// <summary>
    /// Number of errors encountered during crawling
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// List of errors encountered during crawling
    /// </summary>
    public List<CrawlError> Errors { get; set; } = new();

    /// <summary>
    /// Start time of the crawl operation
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// End time of the crawl operation
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Duration of the crawl operation
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Type of crawl operation performed
    /// </summary>
    public CrawlType CrawlType { get; set; }

    /// <summary>
    /// Additional metadata about the crawl operation
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// URLs that were successfully crawled
    /// </summary>
    public List<string> SuccessfulUrls { get; set; } = new();

    /// <summary>
    /// URLs that failed to be crawled
    /// </summary>
    public List<string> FailedUrls { get; set; } = new();
}

/// <summary>
/// Represents an error that occurred during crawling
/// </summary>
public class CrawlError
{
    /// <summary>
    /// URL where the error occurred
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Exception details (if available)
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// HTTP status code (if applicable)
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Timestamp when the error occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Severity level of the error
    /// </summary>
    public ErrorSeverity Severity { get; set; } = ErrorSeverity.Warning;
}

/// <summary>
/// Types of crawling operations
/// </summary>
public enum CrawlType
{
    /// <summary>
    /// Full site crawl
    /// </summary>
    Full,

    /// <summary>
    /// Incremental crawl (only updated content)
    /// </summary>
    Incremental,

    /// <summary>
    /// Products only
    /// </summary>
    ProductsOnly,

    /// <summary>
    /// Categories only
    /// </summary>
    CategoriesOnly,

    /// <summary>
    /// Content pages only
    /// </summary>
    ContentOnly
}

/// <summary>
/// Error severity levels
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// Informational message
    /// </summary>
    Info,

    /// <summary>
    /// Warning that doesn't prevent operation
    /// </summary>
    Warning,

    /// <summary>
    /// Error that affects functionality
    /// </summary>
    Error,

    /// <summary>
    /// Critical error that stops operation
    /// </summary>
    Critical
}