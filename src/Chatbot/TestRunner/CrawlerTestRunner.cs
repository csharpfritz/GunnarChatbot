using Chatbot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Chatbot.TestRunner;

/// <summary>
/// Test runner to demonstrate the web crawler with enhanced logging and metrics
/// </summary>
public class CrawlerTestRunner
{
    private readonly ILogger<CrawlerTestRunner> _logger;
    private readonly IWebCrawlerService _webCrawler;

    public CrawlerTestRunner(ILogger<CrawlerTestRunner> logger, IWebCrawlerService webCrawler)
    {
        _logger = logger;
        _webCrawler = webCrawler;
    }

    /// <summary>
    /// Run the web crawler test with detailed logging and metrics
    /// </summary>
    public async Task RunCrawlerTestAsync()
    {
        var overallStopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("🚀 Starting Web Crawler Test Runner with Enhanced Logging & Metrics");
        _logger.LogInformation("================================================================================");

        try
        {
            // Test 1: Configuration Validation
            _logger.LogInformation("🔍 Test 1: Validating web crawler configuration...");
            var configValidation = await _webCrawler.ValidateConfigurationAsync();
            
            if (!configValidation)
            {
                _logger.LogError("❌ Configuration validation failed. Cannot proceed with crawling tests.");
                return;
            }

            _logger.LogInformation("✅ Configuration validation passed!");
            _logger.LogInformation("");

            // Test 2: Single Product Crawl
            _logger.LogInformation("🎯 Test 2: Crawling single Overwatch Ultimate product...");
            var singleProduct = await _webCrawler.CrawlProductAsync("https://gunnar.com/collections/gaming-glasses/products/overwatch-ultimate");
            
            if (singleProduct != null)
            {
                _logger.LogInformation("✅ Single product crawl successful!");
                _logger.LogInformation("📊 Product Summary:");
                _logger.LogInformation("   • Name: {ProductName}", singleProduct.Name);
                _logger.LogInformation("   • SKU: {SKU}", singleProduct.SKU);
                _logger.LogInformation("   • Category: {Category}", singleProduct.Category);
                _logger.LogInformation("   • Collection: {Collection}", singleProduct.Collection);
                _logger.LogInformation("   • Price: ${Price}", singleProduct.Price);
                _logger.LogInformation("   • Features: {FeatureCount} items", singleProduct.Features.Count);
                _logger.LogInformation("   • Lens Options: {LensCount} types", singleProduct.SupportedLenses.Count);
                _logger.LogInformation("   • Images: {ImageCount} URLs", singleProduct.Images.Count);
                _logger.LogInformation("   • Tags: {TagCount} items", singleProduct.Tags.Count);
                
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("🏷️ Product Tags: [{Tags}]", string.Join(", ", singleProduct.Tags));
                    _logger.LogDebug("👓 Lens Types: [{LensTypes}]", 
                        string.Join(", ", singleProduct.SupportedLenses.Select(l => $"{l.LensType} ({l.BlueLightProtection})")));
                }
            }
            else
            {
                _logger.LogWarning("❌ Single product crawl failed!");
            }

            _logger.LogInformation("");

            // Test 3: Full Products Crawl
            _logger.LogInformation("🎯 Test 3: Running full products crawl operation...");
            var allProducts = await _webCrawler.CrawlProductsAsync();
            
            _logger.LogInformation("✅ Full products crawl completed!");
            _logger.LogInformation("📊 Crawl Results Summary:");
            _logger.LogInformation("   • Total Products Found: {ProductCount}", allProducts.Count);
            _logger.LogInformation("   • Valid Products: {ValidCount}", allProducts.Count(p => !string.IsNullOrEmpty(p.Name)));
            _logger.LogInformation("   • Products with Pricing: {PricedCount}", allProducts.Count(p => p.Price > 0));
            _logger.LogInformation("   • Products with Features: {FeaturedCount}", allProducts.Count(p => p.Features.Any()));
            _logger.LogInformation("   • Products with Lens Options: {LensedCount}", allProducts.Count(p => p.SupportedLenses.Any()));

            if (allProducts.Any())
            {
                var avgFeatures = allProducts.Average(p => p.Features.Count);
                var avgLensOptions = allProducts.Average(p => p.SupportedLenses.Count);
                var avgImages = allProducts.Average(p => p.Images.Count);
                
                _logger.LogInformation("📈 Quality Metrics:");
                _logger.LogInformation("   • Average Features per Product: {AvgFeatures:F1}", avgFeatures);
                _logger.LogInformation("   • Average Lens Options per Product: {AvgLenses:F1}", avgLensOptions);
                _logger.LogInformation("   • Average Images per Product: {AvgImages:F1}", avgImages);
            }

            overallStopwatch.Stop();
            
            _logger.LogInformation("");
            _logger.LogInformation("🎉 Web Crawler Test Runner completed successfully in {Duration}ms!", 
                overallStopwatch.ElapsedMilliseconds);
            _logger.LogInformation("================================================================================");
        }
        catch (Exception ex)
        {
            overallStopwatch.Stop();
            _logger.LogError(ex, "💥 Critical error in Web Crawler Test Runner after {Duration}ms: {ErrorMessage}",
                overallStopwatch.ElapsedMilliseconds, ex.Message);
            
            throw;
        }
    }
}

/// <summary>
/// Program entry point for testing the web crawler
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🔧 Gunnar Chatbot - Web Crawler Test Runner");
        Console.WriteLine("============================================");
        
        // Create a temporary host for testing
        var builder = Host.CreateApplicationBuilder(args);
        
        // Add basic services needed for the test
        builder.Services.AddHttpClient<WebCrawlerService>();
        builder.Services.Configure<WebCrawlerOptions>(options =>
        {
            options.BaseUrl = "https://gunnar.com";
            options.UserAgent = "Gunnar-ChatBot-DataCollector/1.0 (Test)";
            options.RequestDelayMs = 1000;
            options.TimeoutSeconds = 30;
            options.MaxRetries = 2;
        });
        
        builder.Services.AddScoped<IWebCrawlerService, WebCrawlerService>();
        builder.Services.AddScoped<CrawlerTestRunner>();
        
        // Configure logging for detailed output
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options =>
        {
            options.FormatterName = "simple";
        });
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        
        using var host = builder.Build();
        
        try
        {
            var testRunner = host.Services.GetRequiredService<CrawlerTestRunner>();
            await testRunner.RunCrawlerTestAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Test runner failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}