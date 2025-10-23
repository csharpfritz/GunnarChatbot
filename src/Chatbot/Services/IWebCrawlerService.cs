using Chatbot.Data;

namespace Chatbot.Services;

/// <summary>
/// Interface for web crawling operations to collect Gunnar product data
/// </summary>
public interface IWebCrawlerService
{
    /// <summary>
    /// Crawl all available products from the Gunnar website
    /// </summary>
    /// <returns>List of product data</returns>
    Task<List<ProductData>> CrawlProductsAsync();

    /// <summary>
    /// Crawl product categories and collections
    /// </summary>
    /// <returns>List of category data</returns>
    Task<List<CategoryData>> CrawlCategoriesAsync();

    /// <summary>
    /// Crawl support content, guides, and FAQ pages
    /// </summary>
    /// <returns>List of content data</returns>
    Task<List<ContentData>> CrawlSupportContentAsync();

    /// <summary>
    /// Perform an incremental crawl for updated content since the last crawl
    /// </summary>
    /// <param name="lastCrawl">DateTime of the last successful crawl</param>
    /// <returns>Crawl result with updated content</returns>
    Task<CrawlResult> IncrementalCrawlAsync(DateTime lastCrawl);

    /// <summary>
    /// Crawl a specific product by URL
    /// </summary>
    /// <param name="productUrl">URL of the product page to crawl</param>
    /// <returns>Product data or null if crawling fails</returns>
    Task<ProductData?> CrawlProductAsync(string productUrl);

    /// <summary>
    /// Validate the crawling service configuration
    /// </summary>
    /// <returns>True if service is properly configured</returns>
    Task<bool> ValidateConfigurationAsync();
}