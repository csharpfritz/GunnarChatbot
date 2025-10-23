using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Chatbot.Services;
using Chatbot.Data;

namespace Chatbot;

public class Worker(ILogger<Worker> logger, IServiceProvider serviceProvider) : BackgroundService
{
    private readonly TimeSpan _crawlInterval = TimeSpan.FromHours(6); // Crawl every 6 hours
    private DateTime _lastCrawl = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Gunnar Chatbot Worker starting up...");

        // Wait a bit for services to be ready
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        // Validate services on startup
        await ValidateServicesAsync();

        // Run initial crawl
        await PerformInitialCrawlAsync();

        // Main worker loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Check if it's time for a crawl
                if (DateTime.UtcNow - _lastCrawl >= _crawlInterval)
                {
                    logger.LogInformation("Starting periodic crawl...");
                    await PerformPeriodicCrawlAsync();
                    _lastCrawl = DateTime.UtcNow;
                }

                // Log heartbeat
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Worker heartbeat at: {time}", DateTimeOffset.Now);
                }

                // Wait before next iteration
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in worker main loop");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Brief delay on error
            }
        }

        logger.LogInformation("Gunnar Chatbot Worker shutting down...");
    }

    private async Task ValidateServicesAsync()
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            
            // Validate web crawler
            var webCrawler = scope.ServiceProvider.GetRequiredService<IWebCrawlerService>();
            var isValid = await webCrawler.ValidateConfigurationAsync();
            logger.LogInformation("Web crawler validation: {IsValid}", isValid);

            // Validate embedding service
            var embeddingService = scope.ServiceProvider.GetRequiredService<EmbeddingService>();
            var isEmbeddingAvailable = await embeddingService.IsGeneratorAvailableAsync();
            logger.LogInformation("Embedding service validation: {IsAvailable}", isEmbeddingAvailable);

            // Initialize vector service collection
            var vectorService = scope.ServiceProvider.GetRequiredService<VectorService>();
            await vectorService.InitializeCollectionAsync();
            logger.LogInformation("Vector service collection initialized");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating services");
        }
    }

    private async Task PerformInitialCrawlAsync()
    {
        try
        {
            logger.LogInformation("Performing initial product crawl...");
            
            using var scope = serviceProvider.CreateScope();
            var webCrawler = scope.ServiceProvider.GetRequiredService<IWebCrawlerService>();
            var vectorService = scope.ServiceProvider.GetRequiredService<VectorService>();

            // Crawl products
            var products = await webCrawler.CrawlProductsAsync();
            logger.LogInformation("Crawled {Count} products", products.Count);

            // Index products in vector database
            foreach (var product in products)
            {
                await IndexProductAsync(product, vectorService);
            }

            _lastCrawl = DateTime.UtcNow;
            logger.LogInformation("Initial crawl completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during initial crawl");
        }
    }

    private async Task PerformPeriodicCrawlAsync()
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var webCrawler = scope.ServiceProvider.GetRequiredService<IWebCrawlerService>();
            var vectorService = scope.ServiceProvider.GetRequiredService<VectorService>();

            // Perform incremental crawl
            var crawlResult = await webCrawler.IncrementalCrawlAsync(_lastCrawl);
            
            if (crawlResult.Success)
            {
                logger.LogInformation("Periodic crawl completed. Products: {ProductCount}, Errors: {ErrorCount}", 
                    crawlResult.ProductsCrawled, crawlResult.ErrorCount);
            }
            else
            {
                logger.LogWarning("Periodic crawl failed");
                
                // Fallback to full product crawl
                logger.LogInformation("Performing fallback full product crawl...");
                var products = await webCrawler.CrawlProductsAsync();
                
                foreach (var product in products)
                {
                    await IndexProductAsync(product, vectorService);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during periodic crawl");
        }
    }

    private async Task IndexProductAsync(ProductData product, VectorService vectorService)
    {
        var indexStopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            logger.LogDebug("📊 Starting vector indexing for product: {ProductName} (SKU: {SKU})", product.Name, product.SKU);
            
            // Create searchable text content for the product
            var contentStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var productContent = CreateProductSearchContent(product);
            contentStopwatch.Stop();
            
            logger.LogDebug("📝 Generated search content in {Duration}ms - Length: {ContentLength} characters", 
                contentStopwatch.ElapsedMilliseconds, productContent.Length);
            
            // Create metadata for the vector database
            var metadataStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var metadata = new Dictionary<string, Qdrant.Client.Grpc.Value>
            {
                ["product_name"] = product.Name,
                ["sku"] = product.SKU,
                ["category"] = product.Category,
                ["collection"] = product.Collection,
                ["frame_color"] = product.FrameColor,
                ["default_lens_type"] = product.DefaultLensType,
                ["price"] = product.Price.ToString(),
                ["is_available"] = product.IsAvailable.ToString(),
                ["source_url"] = product.SourceUrl,
                ["last_updated"] = product.LastUpdated.ToString("O"),
                ["features_count"] = product.Features.Count.ToString(),
                ["lens_options_count"] = product.SupportedLenses.Count.ToString(),
                ["images_count"] = product.Images.Count.ToString(),
                ["tags_count"] = product.Tags.Count.ToString()
            };

            // Add lens information
            if (product.SupportedLenses.Any())
            {
                var lensTypes = string.Join(", ", product.SupportedLenses.Select(l => l.LensType));
                var blueLightProtections = string.Join(", ", product.SupportedLenses.Select(l => l.BlueLightProtection));
                metadata["supported_lenses"] = lensTypes;
                metadata["blue_light_protections"] = blueLightProtections;
            }

            // Add tags
            if (product.Tags.Any())
            {
                metadata["tags"] = string.Join(", ", product.Tags);
            }

            // Add feature information
            if (product.Features.Any())
            {
                metadata["features"] = string.Join(", ", product.Features.Take(10)); // Limit for metadata size
            }
            
            metadataStopwatch.Stop();
            logger.LogDebug("🏷️ Created metadata in {Duration}ms - {MetadataCount} fields", 
                metadataStopwatch.ElapsedMilliseconds, metadata.Count);

            // Index in vector database
            logger.LogDebug("🔗 Indexing product with SKU '{SKU}' (will be converted to UUID internally)", product.SKU);
            var vectorStopwatch = System.Diagnostics.Stopwatch.StartNew();
            await vectorService.AddProductAsync(product.SKU, productContent, metadata);
            vectorStopwatch.Stop();
            
            indexStopwatch.Stop();
            
            logger.LogInformation("✅ Successfully indexed product '{ProductName}' in vector database - Total: {TotalDuration}ms [Content: {ContentDuration}ms, Metadata: {MetadataDuration}ms, Vector: {VectorDuration}ms]",
                product.Name, indexStopwatch.ElapsedMilliseconds, contentStopwatch.ElapsedMilliseconds, 
                metadataStopwatch.ElapsedMilliseconds, vectorStopwatch.ElapsedMilliseconds);
                
            logger.LogDebug("📈 Indexing metrics - Content length: {ContentLength} chars, Metadata fields: {MetadataFields}, Features: {FeatureCount}, Lenses: {LensCount}, Images: {ImageCount}, Tags: {TagCount}",
                productContent.Length, metadata.Count, product.Features.Count, product.SupportedLenses.Count, product.Images.Count, product.Tags.Count);
        }
        catch (Exception ex)
        {
            indexStopwatch.Stop();
            logger.LogError(ex, "💥 Critical error indexing product '{ProductName}' after {Duration}ms: {ErrorMessage}", 
                product.Name, indexStopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }

    private string CreateProductSearchContent(ProductData product)
    {
        var contentParts = new List<string>();

        // Product name and description
        contentParts.Add($"{product.Name}. {product.Description}");

        // Category and collection
        if (!string.IsNullOrEmpty(product.Category))
        {
            contentParts.Add($"Category: {product.Category}");
        }
        if (!string.IsNullOrEmpty(product.Collection))
        {
            contentParts.Add($"Collection: {product.Collection}");
        }

        // Features
        if (product.Features.Any())
        {
            contentParts.Add($"Features: {string.Join(", ", product.Features)}");
        }

        // Lens information with detailed descriptions
        if (product.SupportedLenses.Any())
        {
            var lensDescriptions = product.SupportedLenses.Select(lens =>
                $"{lens.LensType} lenses ({lens.BlueLightProtection} blue light protection): {lens.Description}. " +
                $"Best for: {string.Join(", ", lens.RecommendedUses)}. " +
                $"Benefits: {string.Join(", ", lens.Benefits)}"
            );
            contentParts.Add($"Available lens options: {string.Join(". ", lensDescriptions)}");
        }

        // Frame information
        if (!string.IsNullOrEmpty(product.FrameColor) || !string.IsNullOrEmpty(product.FrameType))
        {
            contentParts.Add($"Frame: {product.FrameType} frame in {product.FrameColor} color");
        }

        // Tags for additional searchability
        if (product.Tags.Any())
        {
            contentParts.Add($"Tags: {string.Join(", ", product.Tags)}");
        }

        // Price information
        contentParts.Add($"Price: ${product.Price}");

        return string.Join(". ", contentParts.Where(s => !string.IsNullOrEmpty(s)));
    }
}
