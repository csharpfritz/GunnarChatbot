using Chatbot;
using Chatbot.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Add Qdrant client for vector database operations
builder.AddQdrantClient("gunnar-db");

// Add Ollama client for embeddings and AI inference
builder.AddOllamaApiClient("gunnar-ai-nomic-embed-text")
    .AddEmbeddingGenerator();

// Register custom services
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<VectorService>();
builder.Services.AddScoped<IWebCrawlerService, WebCrawlerService>();

// Configure additional telemetry and metrics for web crawler
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("GunnarChatbot.WebCrawler");
    })
    .WithTracing(tracing =>
    {
        tracing.AddSource("GunnarChatbot.WebCrawler");
    });

// Configure web crawler options from configuration
builder.Services.Configure<WebCrawlerOptions>(
    builder.Configuration.GetSection("WebCrawler"));

// Configure HttpClient for web crawling with resilience policies
builder.Services.AddHttpClient<WebCrawlerService>(client =>
{
    client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
    client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
