using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Chatbot.Data;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Trace;

namespace Chatbot.Services;

/// <summary>
/// Configuration options for the web crawler service
/// </summary>
public class WebCrawlerOptions
{
    /// <summary>
    /// Base URL for the Gunnar website
    /// </summary>
    public string BaseUrl { get; set; } = "https://gunnar.com";

    /// <summary>
    /// User agent string for HTTP requests
    /// </summary>
    public string UserAgent { get; set; } = "Gunnar-ChatBot-DataCollector/1.0";

    /// <summary>
    /// Delay between requests in milliseconds (respectful crawling)
    /// </summary>
    public int RequestDelayMs { get; set; } = 1500;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of retries for failed requests
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Web crawler service for collecting Gunnar product data
/// </summary>
public class WebCrawlerService : IWebCrawlerService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebCrawlerService> _logger;
    private readonly WebCrawlerOptions _options;
    
    // Telemetry and metrics
    private static readonly ActivitySource ActivitySource = new("GunnarChatbot.WebCrawler");
    private static readonly Meter Meter = new("GunnarChatbot.WebCrawler");
    
    // Metrics counters and gauges
    private readonly Counter<int> _pagesRequested = Meter.CreateCounter<int>("crawler_pages_requested", "pages", "Number of web pages requested");
    private readonly Counter<int> _pagesSucceeded = Meter.CreateCounter<int>("crawler_pages_succeeded", "pages", "Number of web pages successfully crawled");
    private readonly Counter<int> _pagesFailed = Meter.CreateCounter<int>("crawler_pages_failed", "pages", "Number of web pages that failed to crawl");
    private readonly Counter<int> _productsExtracted = Meter.CreateCounter<int>("crawler_products_extracted", "products", "Number of products successfully extracted");
    private readonly Counter<int> _productsValidated = Meter.CreateCounter<int>("crawler_products_validated", "products", "Number of products that passed validation");
    private readonly Counter<int> _productsFailed = Meter.CreateCounter<int>("crawler_products_failed", "products", "Number of products that failed extraction or validation");
    
    private readonly Histogram<double> _requestDuration = Meter.CreateHistogram<double>("crawler_request_duration", "ms", "Duration of HTTP requests in milliseconds");
    private readonly Histogram<double> _parseDuration = Meter.CreateHistogram<double>("crawler_parse_duration", "ms", "Duration of HTML parsing in milliseconds");
    private readonly Histogram<int> _pageSize = Meter.CreateHistogram<int>("crawler_page_size", "bytes", "Size of crawled pages in bytes");
    private readonly Histogram<int> _extractedFeatures = Meter.CreateHistogram<int>("crawler_extracted_features", "count", "Number of features extracted per product");

    public WebCrawlerService(
        HttpClient httpClient, 
        ILogger<WebCrawlerService> logger,
        IOptions<WebCrawlerOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
        
        ConfigureHttpClient();
        
        _logger.LogInformation("WebCrawlerService initialized with configuration: BaseUrl={BaseUrl}, UserAgent={UserAgent}, RequestDelay={RequestDelayMs}ms, Timeout={TimeoutSeconds}s", 
            _options.BaseUrl, _options.UserAgent, _options.RequestDelayMs, _options.TimeoutSeconds);
    }

    private void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", _options.UserAgent);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    /// <inheritdoc/>
    public async Task<List<ProductData>> CrawlProductsAsync()
    {
        using var activity = ActivitySource.StartActivity("CrawlProducts");
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("Starting comprehensive product crawl operation...");
        activity?.AddTag("crawl.type", "products");
        activity?.AddTag("crawl.scope", "overwatch-ultimate");

        var products = new List<ProductData>();
        var crawlMetrics = new
        {
            StartTime = DateTime.UtcNow,
            AttemptedUrls = 0,
            SuccessfulUrls = 0,
            FailedUrls = 0,
            TotalProducts = 0,
            ValidProducts = 0,
            InvalidProducts = 0
        };

        try
        {
            // For now, we'll focus on the Overwatch Ultimate product as specified
            var targetUrls = new[]
            {
                "https://gunnar.com/collections/shop-all/products/overwatch-ultimate"
            };

            _logger.LogInformation("Targeting {UrlCount} product URLs for crawling", targetUrls.Length);
            activity?.AddTag("crawl.target_urls", targetUrls.Length);

            foreach (var url in targetUrls)
            {
                crawlMetrics = crawlMetrics with { AttemptedUrls = crawlMetrics.AttemptedUrls + 1 };
                
                _logger.LogDebug("Processing product URL: {Url} (Attempt {AttemptNumber}/{TotalUrls})", 
                    url, crawlMetrics.AttemptedUrls, targetUrls.Length);

                var product = await CrawlProductAsync(url);
                
                if (product != null)
                {
                    products.Add(product);
                    crawlMetrics = crawlMetrics with 
                    { 
                        SuccessfulUrls = crawlMetrics.SuccessfulUrls + 1,
                        TotalProducts = crawlMetrics.TotalProducts + 1,
                        ValidProducts = crawlMetrics.ValidProducts + 1
                    };
                    
                    _productsExtracted.Add(1, new KeyValuePair<string, object?>("url", url));
                    _productsValidated.Add(1, new KeyValuePair<string, object?>("product_name", product.Name));
                    
                    // Generate the UUID that will be used in the vector database
                    var vectorUuid = VectorService.GenerateUuidFromString(product.SKU);
                    
                    _logger.LogInformation("✅ Successfully crawled product: {ProductName} (SKU: {SKU}) from {Url}", 
                        product.Name, product.SKU, url);
                        
                    // Log detailed product information
                    _logger.LogDebug("Product details - Name: {Name}, Description: {Description}, Price: ${Price}, Features: [{Features}], SupportedLenses: [{LensTypes}]",
                        product.Name, 
                        product.Description.Length > 100 ? product.Description.Substring(0, 100) + "..." : product.Description,
                        product.Price,
                        string.Join(", ", product.Features.Take(3)),
                        string.Join(", ", product.SupportedLenses.Select(l => l.LensType)));
                        
                    // Log Shopify identifiers
                    if (!string.IsNullOrEmpty(product.ShopifyProductId))
                    {
                        _logger.LogDebug("🛒 Shopify Data - ProductID: {ShopifyProductId}, GID: {ShopifyGid}, SelectedVariant: {SelectedVariant}, Variants: [{Variants}]",
                            product.ShopifyProductId, product.ShopifyGid ?? "N/A", product.SelectedVariantId ?? "N/A",
                            string.Join(", ", product.ShopifyVariantIds.Take(3)));
                    }
                        
                    _logger.LogDebug("🆔 SKU '{SKU}' will be indexed with UUID: {VectorUuid}", product.SKU, vectorUuid);
                }
                else
                {
                    crawlMetrics = crawlMetrics with { FailedUrls = crawlMetrics.FailedUrls + 1 };
                    _productsFailed.Add(1, new KeyValuePair<string, object?>("url", url));
                    
                    _logger.LogWarning("❌ Failed to crawl product from URL: {Url}", url);
                }
            }

            stopwatch.Stop();
            var duration = stopwatch.ElapsedMilliseconds;
            
            // Record comprehensive metrics
            activity?.AddTag("crawl.duration_ms", duration);
            activity?.AddTag("crawl.attempted_urls", crawlMetrics.AttemptedUrls);
            activity?.AddTag("crawl.successful_urls", crawlMetrics.SuccessfulUrls);
            activity?.AddTag("crawl.failed_urls", crawlMetrics.FailedUrls);
            activity?.AddTag("crawl.products_extracted", crawlMetrics.TotalProducts);
            
            _logger.LogInformation("🎉 Product crawl operation completed successfully! Duration: {Duration}ms, URLs: {Successful}/{Attempted}, Products: {ProductCount}",
                duration, crawlMetrics.SuccessfulUrls, crawlMetrics.AttemptedUrls, products.Count);

            // Log performance metrics
            _logger.LogInformation("📊 Crawl Performance Metrics - Average time per URL: {AvgTimePerUrl:F2}ms, Success rate: {SuccessRate:P2}, Products per successful URL: {ProductsPerUrl:F1}",
                crawlMetrics.AttemptedUrls > 0 ? (double)duration / crawlMetrics.AttemptedUrls : 0,
                crawlMetrics.AttemptedUrls > 0 ? (double)crawlMetrics.SuccessfulUrls / crawlMetrics.AttemptedUrls : 0,
                crawlMetrics.SuccessfulUrls > 0 ? (double)crawlMetrics.TotalProducts / crawlMetrics.SuccessfulUrls : 0);

            return products;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "💥 Critical error during product crawl operation after {Duration}ms. Attempted URLs: {Attempted}, Successful: {Successful}", 
                stopwatch.ElapsedMilliseconds, crawlMetrics.AttemptedUrls, crawlMetrics.SuccessfulUrls);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ProductData?> CrawlProductAsync(string productUrl)
    {
        using var activity = ActivitySource.StartActivity("CrawlProduct");
        var overallStopwatch = Stopwatch.StartNew();
        
        activity?.AddTag("product.url", productUrl);
        _pagesRequested.Add(1, new KeyValuePair<string, object?>("url", productUrl));
        
        _logger.LogDebug("🔍 Starting individual product crawl for URL: {Url}", productUrl);

        try
        {
            // Phase 1: Fetch HTML content
            _logger.LogDebug("📥 Phase 1: Fetching HTML content from {Url}", productUrl);
            var fetchStopwatch = Stopwatch.StartNew();
            
            var html = await FetchPageContentAsync(productUrl);
            
            fetchStopwatch.Stop();
            _requestDuration.Record(fetchStopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("url", productUrl));
            
            if (string.IsNullOrEmpty(html))
            {
                _pagesFailed.Add(1, new KeyValuePair<string, object?>("reason", "empty_content"));
                _logger.LogWarning("❌ Phase 1 failed: Empty or null HTML content received from {Url}", productUrl);
                activity?.SetStatus(ActivityStatusCode.Error, "Empty HTML content");
                return null;
            }

            var contentSize = System.Text.Encoding.UTF8.GetByteCount(html);
            _pageSize.Record(contentSize, new KeyValuePair<string, object?>("url", productUrl));
            
            _logger.LogDebug("✅ Phase 1 completed: Fetched {ContentSize:N0} bytes in {Duration}ms from {Url}", 
                contentSize, fetchStopwatch.ElapsedMilliseconds, productUrl);

            // Phase 2: Parse HTML and extract product data
            _logger.LogDebug("🔧 Phase 2: Parsing HTML and extracting product data");
            var parseStopwatch = Stopwatch.StartNew();
            
            var product = ParseProductPage(html, productUrl);
            
            parseStopwatch.Stop();
            _parseDuration.Record(parseStopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("url", productUrl));
            
            if (product == null)
            {
                _pagesFailed.Add(1, new KeyValuePair<string, object?>("reason", "parse_failed"));
                _logger.LogWarning("❌ Phase 2 failed: Product parsing returned null for {Url}", productUrl);
                activity?.SetStatus(ActivityStatusCode.Error, "Product parsing failed");
                return null;
            }

            _logger.LogDebug("✅ Phase 2 completed: Parsed product data in {Duration}ms - Name: {Name}, Features: {FeatureCount}, Lenses: {LensCount}",
                parseStopwatch.ElapsedMilliseconds, product.Name, product.Features.Count, product.SupportedLenses.Count);

            // Record feature extraction metrics
            _extractedFeatures.Record(product.Features.Count, new KeyValuePair<string, object?>("product_name", product.Name));

            // Phase 3: Validation
            _logger.LogDebug("✅ Phase 3: Validating extracted product data");
            var validation = DataValidator.ValidateProduct(product);
            
            if (!validation.IsValid)
            {
                _logger.LogWarning("⚠️ Product validation issues for {Name}: Errors={ErrorCount}, Warnings={WarningCount}", 
                    product.Name, validation.Errors.Count, validation.Warnings.Count);
                
                foreach (var error in validation.Errors.Take(5)) // Limit to first 5 errors
                {
                    _logger.LogWarning("  🔸 Validation error: {Error}", error);
                }
                
                if (validation.Errors.Count > 5)
                {
                    _logger.LogWarning("  ... and {RemainingErrors} more errors", validation.Errors.Count - 5);
                }

                // Try to fix common issues
                _logger.LogDebug("🔧 Attempting to fix common product validation issues...");
                FixCommonProductIssues(product);
                
                var revalidation = DataValidator.ValidateProduct(product);
                if (revalidation.IsValid)
                {
                    _logger.LogInformation("✅ Product validation issues resolved after automatic fixes");
                }
                else
                {
                    _logger.LogWarning("❌ Product validation still has {ErrorCount} errors after fixes", revalidation.Errors.Count);
                }
            }
            else
            {
                _logger.LogDebug("✅ Phase 3 completed: Product data passed validation");
            }

            // Phase 4: Respectful crawling delay
            _logger.LogDebug("⏱️ Phase 4: Applying respectful crawling delay of {DelayMs}ms", _options.RequestDelayMs);
            await Task.Delay(_options.RequestDelayMs);

            overallStopwatch.Stop();
            var totalDuration = overallStopwatch.ElapsedMilliseconds;
            
            // Record success metrics
            _pagesSucceeded.Add(1, new KeyValuePair<string, object?>("url", productUrl));
            
            // Add comprehensive telemetry tags
            activity?.AddTag("product.name", product.Name);
            activity?.AddTag("product.sku", product.SKU);
            activity?.AddTag("product.category", product.Category);
            activity?.AddTag("product.collection", product.Collection);
            activity?.AddTag("product.price", product.Price);
            activity?.AddTag("product.feature_count", product.Features.Count);
            activity?.AddTag("product.lens_count", product.SupportedLenses.Count);
            activity?.AddTag("product.image_count", product.Images.Count);
            activity?.AddTag("crawl.total_duration_ms", totalDuration);
            activity?.AddTag("crawl.fetch_duration_ms", fetchStopwatch.ElapsedMilliseconds);
            activity?.AddTag("crawl.parse_duration_ms", parseStopwatch.ElapsedMilliseconds);
            activity?.AddTag("crawl.content_size_bytes", contentSize);
            activity?.AddTag("validation.is_valid", validation.IsValid);
            activity?.AddTag("validation.error_count", validation.Errors.Count);
            activity?.AddTag("validation.warning_count", validation.Warnings.Count);

            _logger.LogInformation("🎉 Successfully crawled product: {ProductName} (SKU: {SKU}) in {TotalDuration}ms [Fetch: {FetchDuration}ms, Parse: {ParseDuration}ms, Size: {ContentSize:N0} bytes]",
                product.Name, product.SKU, totalDuration, fetchStopwatch.ElapsedMilliseconds, parseStopwatch.ElapsedMilliseconds, contentSize);

            return product;
        }
        catch (Exception ex)
        {
            overallStopwatch.Stop();
            _pagesFailed.Add(1, new KeyValuePair<string, object?>("reason", "exception"));
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            _logger.LogError(ex, "💥 Critical error crawling product from {Url} after {Duration}ms: {ErrorMessage}", 
                productUrl, overallStopwatch.ElapsedMilliseconds, ex.Message);
            
            return null;
        }
    }

    private async Task<string?> FetchPageContentAsync(string url)
    {
        using var activity = ActivitySource.StartActivity("FetchPageContent");
        var stopwatch = Stopwatch.StartNew();
        
        activity?.AddTag("http.url", url);
        activity?.AddTag("http.method", "GET");
        
        _logger.LogDebug("🌐 Initiating HTTP GET request to {Url}", url);

        try
        {
            var response = await _httpClient.GetAsync(url);
            stopwatch.Stop();
            
            var statusCode = (int)response.StatusCode;
            var contentLength = response.Content.Headers.ContentLength ?? 0;
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "unknown";
            
            // Add HTTP response telemetry
            activity?.AddTag("http.status_code", statusCode);
            activity?.AddTag("http.response_size", contentLength);
            activity?.AddTag("http.content_type", contentType);
            activity?.AddTag("http.duration_ms", stopwatch.ElapsedMilliseconds);
            
            _logger.LogDebug("📡 HTTP Response received: Status={StatusCode}, ContentLength={ContentLength:N0} bytes, ContentType={ContentType}, Duration={Duration}ms",
                statusCode, contentLength, contentType, stopwatch.ElapsedMilliseconds);

            // Log response headers for debugging
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("📋 HTTP Response Headers:");
                foreach (var header in response.Headers)
                {
                    _logger.LogTrace("  {HeaderName}: {HeaderValue}", header.Key, string.Join(", ", header.Value));
                }
            }

            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var actualContentSize = System.Text.Encoding.UTF8.GetByteCount(content);
            
            _logger.LogDebug("✅ Successfully fetched and read {ActualSize:N0} bytes of content from {Url} in {Duration}ms",
                actualContentSize, url, stopwatch.ElapsedMilliseconds);
            
            // Validate content quality
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("⚠️ Received empty or whitespace-only content from {Url}", url);
                activity?.AddTag("content.quality", "empty");
                return null;
            }
            
            if (content.Length < 1000)
            {
                _logger.LogWarning("⚠️ Suspiciously small content ({Size} bytes) received from {Url}", content.Length, url);
                activity?.AddTag("content.quality", "suspicious_small");
            }
            
            if (!content.Contains("<html", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("⚠️ Content does not appear to be HTML (missing <html tag) from {Url}", url);
                activity?.AddTag("content.quality", "not_html");
            }
            
            activity?.AddTag("content.quality", "valid");
            activity?.AddTag("content.actual_size", actualContentSize);
            
            return content;
        }
        catch (HttpRequestException httpEx)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, httpEx.Message);
            activity?.AddTag("http.duration_ms", stopwatch.ElapsedMilliseconds);
            
            _logger.LogError(httpEx, "🌐 HTTP request failed for {Url} after {Duration}ms: {ErrorMessage}", 
                url, stopwatch.ElapsedMilliseconds, httpEx.Message);
            
            // Log specific HTTP error details
            if (httpEx.Data.Contains("StatusCode"))
            {
                _logger.LogError("  📊 HTTP Status Code: {StatusCode}", httpEx.Data["StatusCode"]);
            }
            
            return null;
        }
        catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, "Request timeout");
            activity?.AddTag("http.duration_ms", stopwatch.ElapsedMilliseconds);
            
            _logger.LogError("⏱️ HTTP request timed out for {Url} after {Duration}ms (configured timeout: {TimeoutSeconds}s)", 
                url, stopwatch.ElapsedMilliseconds, _options.TimeoutSeconds);
            
            return null;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddTag("http.duration_ms", stopwatch.ElapsedMilliseconds);
            
            _logger.LogError(ex, "💥 Unexpected error fetching page content from {Url} after {Duration}ms: {ErrorMessage}", 
                url, stopwatch.ElapsedMilliseconds, ex.Message);
            
            return null;
        }
    }

    private ProductData? ParseProductPage(string html, string sourceUrl)
    {
        using var activity = ActivitySource.StartActivity("ParseProductPage");
        var stopwatch = Stopwatch.StartNew();
        
        activity?.AddTag("parse.url", sourceUrl);
        activity?.AddTag("parse.html_size", html.Length);
        
        _logger.LogDebug("🔧 Starting HTML parsing for {Url} - HTML size: {HtmlSize:N0} characters", sourceUrl, html.Length);

        try
        {
            // Phase 1: Load HTML document
            var loadStopwatch = Stopwatch.StartNew();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            loadStopwatch.Stop();
            
            _logger.LogDebug("📄 HTML document loaded in {Duration}ms - Total nodes: {NodeCount}", 
                loadStopwatch.ElapsedMilliseconds, doc.DocumentNode.DescendantsAndSelf().Count());

            var product = new ProductData
            {
                SourceUrl = sourceUrl,
                LastUpdated = DateTime.UtcNow
            };

            var extractionMetrics = new
            {
                BasicInfoDuration = 0L,
                LensDuration = 0L,
                SpecificationsDuration = 0L,
                ImagesDuration = 0L,
                CategorizationDuration = 0L,
                ValidationDuration = 0L
            };

            // Phase 2: Extract basic product information
            _logger.LogDebug("🏷️ Phase 2a: Extracting basic product information");
            var basicStopwatch = Stopwatch.StartNew();
            ExtractBasicProductInfo(doc, product);
            basicStopwatch.Stop();
            extractionMetrics = extractionMetrics with { BasicInfoDuration = basicStopwatch.ElapsedMilliseconds };
            
            _logger.LogDebug("✅ Basic info extracted in {Duration}ms - Name: '{Name}', SKU: '{SKU}', Price: ${Price}",
                basicStopwatch.ElapsedMilliseconds, product.Name ?? "N/A", product.SKU ?? "N/A", product.Price);
            
            // Phase 3: Extract lens information
            _logger.LogDebug("👓 Phase 2b: Extracting lens information");
            var lensStopwatch = Stopwatch.StartNew();
            ExtractLensInformation(doc, product);
            lensStopwatch.Stop();
            extractionMetrics = extractionMetrics with { LensDuration = lensStopwatch.ElapsedMilliseconds };
            
            _logger.LogDebug("✅ Lens info extracted in {Duration}ms - Default: {DefaultLens}, Supported: [{SupportedLenses}]",
                lensStopwatch.ElapsedMilliseconds, 
                product.DefaultLensType ?? "N/A", 
                string.Join(", ", product.SupportedLenses.Select(l => $"{l.LensType}({l.BlueLightProtection})")));
            
            // Phase 4: Extract specifications
            _logger.LogDebug("📋 Phase 2c: Extracting product specifications");
            var specStopwatch = Stopwatch.StartNew();
            ExtractSpecifications(doc, product);
            specStopwatch.Stop();
            extractionMetrics = extractionMetrics with { SpecificationsDuration = specStopwatch.ElapsedMilliseconds };
            
            _logger.LogDebug("✅ Specifications extracted in {Duration}ms - Count: {SpecCount}, Frame: {FrameType}/{FrameColor}",
                specStopwatch.ElapsedMilliseconds, product.Specifications.Count, 
                product.FrameType ?? "N/A", product.FrameColor ?? "N/A");
            
            // Phase 5: Extract images
            _logger.LogDebug("🖼️ Phase 2d: Extracting product images");
            var imageStopwatch = Stopwatch.StartNew();
            ExtractProductImages(doc, product);
            imageStopwatch.Stop();
            extractionMetrics = extractionMetrics with { ImagesDuration = imageStopwatch.ElapsedMilliseconds };
            
            _logger.LogDebug("✅ Images extracted in {Duration}ms - Image count: {ImageCount}",
                imageStopwatch.ElapsedMilliseconds, product.Images.Count);
            
            // Phase 6: Set category and collection information
            _logger.LogDebug("🏷️ Phase 2e: Setting product categorization");
            var catStopwatch = Stopwatch.StartNew();
            SetProductCategorization(product, sourceUrl);
            catStopwatch.Stop();
            extractionMetrics = extractionMetrics with { CategorizationDuration = catStopwatch.ElapsedMilliseconds };
            
            _logger.LogDebug("✅ Categorization completed in {Duration}ms - Category: {Category}, Collection: {Collection}, Tags: [{Tags}]",
                catStopwatch.ElapsedMilliseconds, product.Category ?? "N/A", product.Collection ?? "N/A",
                string.Join(", ", product.Tags.Take(5)));

            // Phase 7: Validate the extracted product data
            _logger.LogDebug("✅ Phase 2f: Validating extracted product data");
            var validationStopwatch = Stopwatch.StartNew();
            var validation = DataValidator.ValidateProduct(product);
            validationStopwatch.Stop();
            extractionMetrics = extractionMetrics with { ValidationDuration = validationStopwatch.ElapsedMilliseconds };
            
            if (!validation.IsValid)
            {
                _logger.LogWarning("⚠️ Product validation failed for '{Name}' - Errors: {ErrorCount}, Warnings: {WarningCount}", 
                    product.Name, validation.Errors.Count, validation.Warnings.Count);
                
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    foreach (var error in validation.Errors.Take(3))
                    {
                        _logger.LogDebug("  🔸 Error: {Error}", error);
                    }
                    foreach (var warning in validation.Warnings.Take(2))
                    {
                        _logger.LogDebug("  ⚠️ Warning: {Warning}", warning);
                    }
                }
                
                // Try to fix common issues
                _logger.LogDebug("🔧 Attempting to fix common validation issues...");
                FixCommonProductIssues(product);
                
                var revalidation = DataValidator.ValidateProduct(product);
                if (revalidation.IsValid)
                {
                    _logger.LogDebug("✅ Validation issues resolved after fixes");
                }
                else
                {
                    _logger.LogWarning("❌ Still has {ErrorCount} validation errors after fixes", revalidation.Errors.Count);
                }
            }
            else
            {
                _logger.LogDebug("✅ Product validation passed on first attempt");
            }

            stopwatch.Stop();
            var totalDuration = stopwatch.ElapsedMilliseconds;

            // Record comprehensive parsing metrics
            activity?.AddTag("parse.total_duration_ms", totalDuration);
            activity?.AddTag("parse.basic_info_duration_ms", extractionMetrics.BasicInfoDuration);
            activity?.AddTag("parse.lens_duration_ms", extractionMetrics.LensDuration);
            activity?.AddTag("parse.specs_duration_ms", extractionMetrics.SpecificationsDuration);
            activity?.AddTag("parse.images_duration_ms", extractionMetrics.ImagesDuration);
            activity?.AddTag("parse.categorization_duration_ms", extractionMetrics.CategorizationDuration);
            activity?.AddTag("parse.validation_duration_ms", extractionMetrics.ValidationDuration);
            activity?.AddTag("parse.extracted_features", product.Features.Count);
            activity?.AddTag("parse.extracted_lenses", product.SupportedLenses.Count);
            activity?.AddTag("parse.extracted_images", product.Images.Count);
            activity?.AddTag("parse.extracted_specs", product.Specifications.Count);
            activity?.AddTag("parse.validation_errors", validation.Errors.Count);
            activity?.AddTag("parse.validation_warnings", validation.Warnings.Count);

            _logger.LogInformation("🎉 Successfully parsed product '{Name}' in {TotalDuration}ms [Load: {LoadDuration}ms, Basic: {BasicDuration}ms, Lens: {LensDuration}ms, Specs: {SpecsDuration}ms, Images: {ImagesDuration}ms, Cat: {CatDuration}ms, Val: {ValDuration}ms]",
                product.Name, totalDuration, loadStopwatch.ElapsedMilliseconds,
                extractionMetrics.BasicInfoDuration, extractionMetrics.LensDuration, 
                extractionMetrics.SpecificationsDuration, extractionMetrics.ImagesDuration,
                extractionMetrics.CategorizationDuration, extractionMetrics.ValidationDuration);

            return product;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddTag("parse.duration_ms", stopwatch.ElapsedMilliseconds);
            
            _logger.LogError(ex, "💥 Critical error parsing product page from {Url} after {Duration}ms: {ErrorMessage}", 
                sourceUrl, stopwatch.ElapsedMilliseconds, ex.Message);
            
            return null;
        }
    }

    private void ExtractShopifyProductData(HtmlDocument doc, ProductData product)
    {
        using var activity = ActivitySource.StartActivity("ExtractShopifyData");
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogDebug("🛒 Extracting Shopify product data from JavaScript");
        
        try
        {
            // Look for Shopify Analytics data in script tags
            var scriptNodes = doc.DocumentNode.SelectNodes("//script[not(@src)]");
            
            if (scriptNodes != null)
            {
                foreach (var script in scriptNodes)
                {
                    var scriptContent = script.InnerText;
                    
                    // Look for ShopifyAnalytics.meta pattern
                    var shopifyAnalyticsMatch = Regex.Match(scriptContent, 
                        @"ShopifyAnalytics\.meta\s*=\s*(\{.*?\});", 
                        RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    
                    if (shopifyAnalyticsMatch.Success)
                    {
                        try
                        {
                            var jsonData = shopifyAnalyticsMatch.Groups[1].Value;
                            using var document = JsonDocument.Parse(jsonData);
                            var root = document.RootElement;
                            
                            // Extract product information
                            if (root.TryGetProperty("product", out var productElement))
                            {
                                // Extract Shopify Product ID
                                if (productElement.TryGetProperty("id", out var idElement))
                                {
                                    product.ShopifyProductId = idElement.GetInt64().ToString();
                                    activity?.AddTag("shopify.product_id", product.ShopifyProductId);
                                }
                                
                                // Extract GID (Global ID)
                                if (productElement.TryGetProperty("gid", out var gidElement))
                                {
                                    var gid = gidElement.GetString();
                                    if (!string.IsNullOrEmpty(gid))
                                    {
                                        product.ShopifyGid = gid;
                                        activity?.AddTag("shopify.gid", product.ShopifyGid);
                                    }
                                }
                                
                                // Extract vendor
                                if (productElement.TryGetProperty("vendor", out var vendorElement))
                                {
                                    var vendor = vendorElement.GetString();
                                    if (!string.IsNullOrEmpty(vendor))
                                    {
                                        product.Vendor = vendor;
                                    }
                                }
                                
                                // Extract type/category
                                if (productElement.TryGetProperty("type", out var typeElement))
                                {
                                    var productType = typeElement.GetString();
                                    if (!string.IsNullOrEmpty(productType))
                                    {
                                        product.Category = productType;
                                    }
                                }
                                
                                // Extract variants
                                if (productElement.TryGetProperty("variants", out var variantsElement) && 
                                    variantsElement.ValueKind == JsonValueKind.Array)
                                {
                                    var variants = new List<string>();
                                    var skus = new List<string>();
                                    
                                    foreach (var variant in variantsElement.EnumerateArray())
                                    {
                                        if (variant.TryGetProperty("id", out var variantIdElement))
                                        {
                                            variants.Add(variantIdElement.GetInt64().ToString());
                                        }
                                        
                                        if (variant.TryGetProperty("sku", out var skuElement))
                                        {
                                            var sku = skuElement.GetString();
                                            if (!string.IsNullOrEmpty(sku))
                                            {
                                                skus.Add(sku);
                                            }
                                        }
                                        
                                        // Use the first variant's SKU as the primary SKU
                                        if (string.IsNullOrEmpty(product.SKU) && 
                                            variant.TryGetProperty("sku", out var primarySkuElement))
                                        {
                                            var skuValue = primarySkuElement.GetString();
                                            if (!string.IsNullOrEmpty(skuValue))
                                            {
                                                product.SKU = skuValue;
                                            }
                                        }
                                        
                                        // Extract price from first variant
                                        if (product.Price == 0 && 
                                            variant.TryGetProperty("price", out var priceElement))
                                        {
                                            // Shopify stores price in cents
                                            var priceInCents = priceElement.GetInt32();
                                            product.Price = priceInCents / 100.0m;
                                        }
                                        
                                        // Extract variant name for product name if not set
                                        if (string.IsNullOrEmpty(product.Name) && 
                                            variant.TryGetProperty("name", out var nameElement))
                                        {
                                            var fullName = nameElement.GetString();
                                            if (!string.IsNullOrEmpty(fullName))
                                            {
                                                // Extract base product name (before the first dash/variant info)
                                                var baseName = fullName.Split('-')[0].Trim();
                                                product.Name = baseName;
                                            }
                                        }
                                    }
                                    
                                    product.ShopifyVariantIds = variants;
                                    
                                    _logger.LogDebug("✅ Extracted Shopify data - ProductId: {ProductId}, SKU: {SKU}, Variants: {VariantCount}, Price: ${Price}",
                                        product.ShopifyProductId, product.SKU, variants.Count, product.Price);
                                        
                                    activity?.AddTag("shopify.variant_count", variants.Count);
                                    activity?.AddTag("shopify.sku_count", skus.Count);
                                }
                            }
                            
                            // Extract selected variant ID
                            if (root.TryGetProperty("selectedVariantId", out var selectedVariantElement))
                            {
                                var selectedVariantId = selectedVariantElement.GetString();
                                if (!string.IsNullOrEmpty(selectedVariantId))
                                {
                                    product.SelectedVariantId = selectedVariantId;
                                    activity?.AddTag("shopify.selected_variant_id", product.SelectedVariantId);
                                }
                            }
                            
                            stopwatch.Stop();
                            activity?.AddTag("extraction.duration_ms", stopwatch.ElapsedMilliseconds);
                            activity?.AddTag("extraction.success", true);
                            
                            _logger.LogInformation("🎉 Successfully extracted Shopify product data in {Duration}ms - ID: {ProductId}, SKU: {SKU}, Name: '{Name}'",
                                stopwatch.ElapsedMilliseconds, product.ShopifyProductId, product.SKU, product.Name);
                            
                            return; // Successfully extracted, no need to continue searching
                        }
                        catch (JsonException jsonEx)
                        {
                            _logger.LogWarning("⚠️ Failed to parse Shopify JSON data: {Error}", jsonEx.Message);
                            activity?.AddTag("extraction.json_error", jsonEx.Message);
                        }
                    }
                }
            }
            
            stopwatch.Stop();
            activity?.AddTag("extraction.duration_ms", stopwatch.ElapsedMilliseconds);
            activity?.AddTag("extraction.success", false);
            
            _logger.LogWarning("⚠️ No Shopify Analytics data found in page scripts after {Duration}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddTag("extraction.duration_ms", stopwatch.ElapsedMilliseconds);
            
            _logger.LogError(ex, "💥 Error extracting Shopify product data after {Duration}ms: {ErrorMessage}", 
                stopwatch.ElapsedMilliseconds, ex.Message);
        }
    }

    private void ExtractBasicProductInfo(HtmlDocument doc, ProductData product)
    {
        // First, try to extract Shopify product data from JavaScript
        ExtractShopifyProductData(doc, product);
        
        // Extract product name (fallback to HTML if not found in Shopify data)
        if (string.IsNullOrEmpty(product.Name))
        {
            var nameNode = doc.DocumentNode.SelectSingleNode("//h1[@class='product-title']") ??
                          doc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'product')]") ??
                          doc.DocumentNode.SelectSingleNode("//h1");
            
            if (nameNode != null)
            {
                product.Name = CleanText(nameNode.InnerText);
            }
        }

        // Extract SKU - prioritize Shopify data, then look for HTML elements
        if (string.IsNullOrEmpty(product.SKU))
        {
            var skuNode = doc.DocumentNode.SelectSingleNode("//span[contains(@class, 'sku')]") ??
                         doc.DocumentNode.SelectSingleNode("//*[contains(text(), 'SKU')]");
            
            if (skuNode != null)
            {
                var skuText = CleanText(skuNode.InnerText);
                var skuMatch = Regex.Match(skuText, @"SKU:?\s*([A-Z0-9-]+)", RegexOptions.IgnoreCase);
                if (skuMatch.Success)
                {
                    product.SKU = skuMatch.Groups[1].Value;
                }
            }
        }

        // Only generate SKU as last resort
        if (string.IsNullOrEmpty(product.SKU) && !string.IsNullOrEmpty(product.Name))
        {
            var urlParts = product.SourceUrl.Split('/');
            var productSlug = urlParts.LastOrDefault() ?? "";
            product.SKU = GenerateSkuFromName(product.Name, productSlug);
            _logger.LogWarning("⚠️ Generated fallback SKU '{SKU}' for product '{Name}' - Shopify data not found", product.SKU, product.Name);
        }

        // Extract description
        var descriptionNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'product-description')]") ??
                             doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'description')]") ??
                             doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
        
        if (descriptionNode != null)
        {
            if (descriptionNode.Name == "meta")
            {
                product.Description = descriptionNode.GetAttributeValue("content", "");
            }
            else
            {
                product.Description = CleanText(descriptionNode.InnerText);
            }
        }

        // Extract price
        var priceNode = doc.DocumentNode.SelectSingleNode("//span[contains(@class, 'price')]") ??
                       doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'money')]");
        
        if (priceNode != null)
        {
            var priceText = CleanText(priceNode.InnerText);
            var priceMatch = Regex.Match(priceText, @"\$?(\d+(?:\.\d{2})?)", RegexOptions.IgnoreCase);
            if (priceMatch.Success && decimal.TryParse(priceMatch.Groups[1].Value, out var price))
            {
                product.Price = price;
            }
        }

        // Extract features from bullet points or feature lists
        var featureNodes = doc.DocumentNode.SelectNodes("//ul[contains(@class, 'features')]//li") ??
                          doc.DocumentNode.SelectNodes("//div[contains(@class, 'features')]//li") ??
                          doc.DocumentNode.SelectNodes("//ul//li[contains(text(), 'light') or contains(text(), 'protection') or contains(text(), 'lens')]");

        if (featureNodes != null)
        {
            foreach (var featureNode in featureNodes)
            {
                var feature = CleanText(featureNode.InnerText);
                if (!string.IsNullOrWhiteSpace(feature) && feature.Length > 3)
                {
                    product.Features.Add(feature);
                }
            }
        }
    }

    private void ExtractLensInformation(HtmlDocument doc, ProductData product)
    {
        // For Overwatch Ultimate, we know it typically comes with specific lens options
        // This is a starting point - we'll enhance this as we learn the page structure
        
        // Default lens type - typically Amber for gaming glasses
        product.DefaultLensType = "Amber";
        
        // Create lens options based on typical Gunnar offerings
        var amberLens = new LensOption
        {
            LensType = "Amber",
            BlueLightProtection = "65%",
            PriceModifier = 0,
            IsAvailable = true,
            Description = "Amber tinted lenses optimized for gaming with enhanced contrast and 65% blue light protection",
            Benefits = new List<string> { "Enhanced Contrast", "Reduced Eye Strain", "Gaming Optimized" },
            RecommendedUses = new List<string> { "Gaming", "Long Gaming Sessions", "Low Light Gaming" },
            ColorEnhancement = "High contrast, warm tint"
        };

        var clearLens = new LensOption
        {
            LensType = "Clear",
            BlueLightProtection = "35%",
            PriceModifier = 0,
            IsAvailable = true,
            Description = "Clear lenses with natural color accuracy and 35% blue light protection",
            Benefits = new List<string> { "Natural Color Accuracy", "All-Day Comfort", "Professional Use" },
            RecommendedUses = new List<string> { "Office Work", "General Computer Use", "Video Calls" },
            ColorEnhancement = "Natural color accuracy"
        };

        product.SupportedLenses.Add(amberLens);
        product.SupportedLenses.Add(clearLens);

        // Look for lens information on the page
        var lensNodes = doc.DocumentNode.SelectNodes("//*[contains(text(), 'lens') or contains(text(), 'blue light')]");
        if (lensNodes != null)
        {
            foreach (var node in lensNodes)
            {
                var text = CleanText(node.InnerText).ToLowerInvariant();
                
                // Extract blue light protection percentage
                var blueProtectionMatch = Regex.Match(text, @"(\d+)%\s*blue\s*light", RegexOptions.IgnoreCase);
                if (blueProtectionMatch.Success)
                {
                    var percentage = blueProtectionMatch.Groups[1].Value + "%";
                    
                    // Update the appropriate lens option
                    if (text.Contains("amber") || text.Contains("yellow"))
                    {
                        amberLens.BlueLightProtection = percentage;
                    }
                    else if (text.Contains("clear") || text.Contains("transparent"))
                    {
                        clearLens.BlueLightProtection = percentage;
                    }
                }
            }
        }
    }

    private void ExtractSpecifications(HtmlDocument doc, ProductData product)
    {
        // Look for specification tables or lists
        var specNodes = doc.DocumentNode.SelectNodes("//table[contains(@class, 'spec')]//tr") ??
                       doc.DocumentNode.SelectNodes("//div[contains(@class, 'specification')]//div");

        if (specNodes != null)
        {
            foreach (var node in specNodes)
            {
                var text = CleanText(node.InnerText);
                
                // Try to extract key-value pairs
                if (text.Contains(":"))
                {
                    var parts = text.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        
                        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                        {
                            product.Specifications[key] = value;
                        }
                    }
                }
            }
        }

        // Extract frame information
        var frameNodes = doc.DocumentNode.SelectNodes("//*[contains(text(), 'frame') or contains(text(), 'material')]");
        if (frameNodes != null)
        {
            foreach (var node in frameNodes)
            {
                var text = CleanText(node.InnerText);
                
                if (text.ToLowerInvariant().Contains("frame"))
                {
                    if (string.IsNullOrEmpty(product.FrameType))
                    {
                        product.FrameType = "Full Frame"; // Default for gaming glasses
                    }
                    
                    // Extract frame color
                    var colorMatch = Regex.Match(text, @"(black|onyx|gunmetal|silver|gold|bronze|copper|tortoise|clear)", RegexOptions.IgnoreCase);
                    if (colorMatch.Success)
                    {
                        product.FrameColor = colorMatch.Groups[1].Value;
                    }
                }
            }
        }
    }

    private void ExtractProductImages(HtmlDocument doc, ProductData product)
    {
        // Look for product images
        var imageNodes = doc.DocumentNode.SelectNodes("//img[contains(@src, 'product') or contains(@alt, 'product')]") ??
                        doc.DocumentNode.SelectNodes("//img[contains(@class, 'product')]");

        if (imageNodes != null)
        {
            foreach (var imgNode in imageNodes)
            {
                var src = imgNode.GetAttributeValue("src", "");
                var dataSrc = imgNode.GetAttributeValue("data-src", ""); // For lazy-loaded images
                
                var imageUrl = !string.IsNullOrEmpty(dataSrc) ? dataSrc : src;
                
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    // Convert relative URLs to absolute
                    if (imageUrl.StartsWith("//"))
                    {
                        imageUrl = "https:" + imageUrl;
                    }
                    else if (imageUrl.StartsWith("/"))
                    {
                        imageUrl = _options.BaseUrl + imageUrl;
                    }
                    
                    if (!product.Images.Contains(imageUrl))
                    {
                        product.Images.Add(imageUrl);
                    }
                }
            }
        }
    }

    private void SetProductCategorization(ProductData product, string sourceUrl)
    {
        // Based on the URL structure, set category and collection
        if (sourceUrl.Contains("/gaming-glasses/"))
        {
            product.Category = "Gaming Glasses";
        }
        
        if (sourceUrl.Contains("overwatch"))
        {
            product.Collection = "Overwatch";
            product.Tags.Add("Overwatch");
            product.Tags.Add("Blizzard");
            product.Tags.Add("Gaming");
            product.Tags.Add("Esports");
        }
        
        // Add general gaming tags
        product.Tags.Add("Gaming Glasses");
        product.Tags.Add("Blue Light Protection");
        product.Tags.Add("Computer Glasses");
    }

    private void FixCommonProductIssues(ProductData product)
    {
        // Fix missing required fields with defaults
        if (string.IsNullOrEmpty(product.Name))
        {
            product.Name = "Gunnar Gaming Glasses";
        }
        
        if (string.IsNullOrEmpty(product.SKU))
        {
            product.SKU = GenerateSkuFromName(product.Name, "");
        }
        
        if (string.IsNullOrEmpty(product.Description))
        {
            product.Description = $"{product.Name} - Gaming glasses designed to reduce eye strain and enhance visual performance.";
        }
        
        if (string.IsNullOrEmpty(product.Category))
        {
            product.Category = "Gaming Glasses";
        }
        
        if (string.IsNullOrEmpty(product.DefaultLensType) && product.SupportedLenses.Any())
        {
            product.DefaultLensType = product.SupportedLenses.First().LensType;
        }
    }

    private string GenerateSkuFromName(string productName, string urlSlug)
    {
        // Create a SKU from product name
        var sku = Regex.Replace(productName, @"[^a-zA-Z0-9\s-]", "")
                      .Replace(" ", "-")
                      .ToUpperInvariant();
        
        if (!string.IsNullOrEmpty(urlSlug))
        {
            sku = urlSlug.ToUpperInvariant().Replace("-", "");
        }
        
        return sku.Length > 20 ? sku.Substring(0, 20) : sku;
    }

    private string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        
        // Remove extra whitespace and decode HTML entities
        text = System.Net.WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }

    /// <inheritdoc/>
    public async Task<List<CategoryData>> CrawlCategoriesAsync()
    {
        _logger.LogInformation("Category crawling not yet implemented");
        return new List<CategoryData>();
    }

    /// <inheritdoc/>
    public async Task<List<ContentData>> CrawlSupportContentAsync()
    {
        _logger.LogInformation("Support content crawling not yet implemented");
        return new List<ContentData>();
    }

    /// <inheritdoc/>
    public async Task<CrawlResult> IncrementalCrawlAsync(DateTime lastCrawl)
    {
        _logger.LogInformation("Incremental crawling not yet implemented");
        
        return new CrawlResult
        {
            Success = false,
            CrawlType = CrawlType.Incremental,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow
        };
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateConfigurationAsync()
    {
        using var activity = ActivitySource.StartActivity("ValidateConfiguration");
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("🔍 Starting web crawler configuration validation...");
        
        activity?.AddTag("validation.base_url", _options.BaseUrl);
        activity?.AddTag("validation.user_agent", _options.UserAgent);
        activity?.AddTag("validation.timeout_seconds", _options.TimeoutSeconds);
        
        try
        {
            // Test 1: Basic connectivity to the Gunnar website
            _logger.LogDebug("🌐 Test 1: Testing basic connectivity to {BaseUrl}", _options.BaseUrl);
            var connectivityStopwatch = Stopwatch.StartNew();
            
            var response = await _httpClient.GetAsync(_options.BaseUrl);
            connectivityStopwatch.Stop();
            
            var statusCode = (int)response.StatusCode;
            var isSuccessful = response.IsSuccessStatusCode;
            
            activity?.AddTag("validation.connectivity_status_code", statusCode);
            activity?.AddTag("validation.connectivity_duration_ms", connectivityStopwatch.ElapsedMilliseconds);
            activity?.AddTag("validation.connectivity_success", isSuccessful);
            
            if (isSuccessful)
            {
                _logger.LogInformation("✅ Test 1 passed: Successfully connected to {BaseUrl} (Status: {StatusCode}) in {Duration}ms",
                    _options.BaseUrl, statusCode, connectivityStopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogError("❌ Test 1 failed: HTTP {StatusCode} from {BaseUrl} in {Duration}ms",
                    statusCode, _options.BaseUrl, connectivityStopwatch.ElapsedMilliseconds);
                return false;
            }

            // Test 2: Response content validation
            _logger.LogDebug("📄 Test 2: Validating response content structure");
            var content = await response.Content.ReadAsStringAsync();
            var contentSize = content?.Length ?? 0;
            var isHtml = content?.Contains("<html", StringComparison.OrdinalIgnoreCase) ?? false;
            var hasGunnarContent = content?.Contains("gunnar", StringComparison.OrdinalIgnoreCase) ?? false;
            
            activity?.AddTag("validation.content_size", contentSize);
            activity?.AddTag("validation.is_html", isHtml);
            activity?.AddTag("validation.has_gunnar_content", hasGunnarContent);
            
            if (contentSize == 0)
            {
                _logger.LogWarning("⚠️ Test 2 warning: Received empty content from {BaseUrl}", _options.BaseUrl);
            }
            else if (!isHtml)
            {
                _logger.LogWarning("⚠️ Test 2 warning: Content does not appear to be HTML ({Size} bytes)", contentSize);
            }
            else if (!hasGunnarContent)
            {
                _logger.LogWarning("⚠️ Test 2 warning: Content does not contain 'gunnar' text ({Size} bytes)", contentSize);
            }
            else
            {
                _logger.LogInformation("✅ Test 2 passed: Valid HTML content with Gunnar branding ({Size:N0} bytes)", contentSize);
            }

            // Test 3: HTTP client configuration validation
            _logger.LogDebug("⚙️ Test 3: Validating HTTP client configuration");
            var userAgent = _httpClient.DefaultRequestHeaders.UserAgent.ToString();
            var hasUserAgent = !string.IsNullOrEmpty(userAgent);
            var timeout = _httpClient.Timeout;
            var expectedTimeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
            var timeoutMatches = Math.Abs((timeout - expectedTimeout).TotalSeconds) < 1;
            
            activity?.AddTag("validation.has_user_agent", hasUserAgent);
            activity?.AddTag("validation.timeout_configured", timeoutMatches);
            
            if (!hasUserAgent)
            {
                _logger.LogWarning("⚠️ Test 3 warning: No User-Agent header configured");
            }
            else
            {
                _logger.LogDebug("✅ User-Agent configured: {UserAgent}", userAgent);
            }
            
            if (!timeoutMatches)
            {
                _logger.LogWarning("⚠️ Test 3 warning: Timeout mismatch - Expected: {Expected}s, Actual: {Actual}s",
                    _options.TimeoutSeconds, timeout.TotalSeconds);
            }
            else
            {
                _logger.LogDebug("✅ Timeout properly configured: {Timeout}s", timeout.TotalSeconds);
            }

            stopwatch.Stop();
            var totalDuration = stopwatch.ElapsedMilliseconds;
            
            activity?.AddTag("validation.total_duration_ms", totalDuration);
            activity?.AddTag("validation.overall_success", isSuccessful);

            _logger.LogInformation("🎉 Web crawler configuration validation completed in {Duration}ms - Status: {Status}",
                totalDuration, isSuccessful ? "PASSED" : "FAILED");
            
            // Log configuration summary
            _logger.LogInformation("📋 Configuration Summary - BaseUrl: {BaseUrl}, UserAgent: '{UserAgent}', Timeout: {Timeout}s, RequestDelay: {DelayMs}ms, MaxRetries: {MaxRetries}",
                _options.BaseUrl, _options.UserAgent, _options.TimeoutSeconds, _options.RequestDelayMs, _options.MaxRetries);

            return isSuccessful;
        }
        catch (HttpRequestException httpEx)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, httpEx.Message);
            activity?.AddTag("validation.duration_ms", stopwatch.ElapsedMilliseconds);
            
            _logger.LogError(httpEx, "🌐 HTTP connectivity test failed for {BaseUrl} after {Duration}ms: {ErrorMessage}",
                _options.BaseUrl, stopwatch.ElapsedMilliseconds, httpEx.Message);
            
            return false;
        }
        catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, "Timeout");
            activity?.AddTag("validation.duration_ms", stopwatch.ElapsedMilliseconds);
            
            _logger.LogError("⏱️ Configuration validation timed out for {BaseUrl} after {Duration}ms (configured timeout: {TimeoutSeconds}s)",
                _options.BaseUrl, stopwatch.ElapsedMilliseconds, _options.TimeoutSeconds);
            
            return false;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddTag("validation.duration_ms", stopwatch.ElapsedMilliseconds);
            
            _logger.LogError(ex, "💥 Unexpected error during configuration validation for {BaseUrl} after {Duration}ms: {ErrorMessage}",
                _options.BaseUrl, stopwatch.ElapsedMilliseconds, ex.Message);
            
            return false;
        }
    }

    /// <summary>
    /// Dispose of resources used by the web crawler service
    /// </summary>
    public void Dispose()
    {
        _logger.LogDebug("🔄 Disposing WebCrawlerService resources...");
        
        try
        {
            ActivitySource?.Dispose();
            Meter?.Dispose();
            
            _logger.LogDebug("✅ WebCrawlerService resources disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error disposing WebCrawlerService resources: {ErrorMessage}", ex.Message);
        }
    }
}