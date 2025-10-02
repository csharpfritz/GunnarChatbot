using Chatbot;
using Chatbot.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Add Qdrant client for vector database operations
builder.AddQdrantClient("gunnar-db");

// Add Ollama client for embeddings and AI inference
builder.AddOllamaClientApi("gunnar-ai")
    .AddEmbeddingGenerator();

// Register custom services
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<VectorService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
