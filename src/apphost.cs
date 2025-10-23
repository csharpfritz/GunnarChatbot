#:package Aspire.Hosting.Qdrant@9.5.0
#:package CommunityToolkit.Aspire.Hosting.Ollama@9.8.0
#:sdk Aspire.AppHost.Sdk@9.5.0
#:project Chatbot

var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddQdrant("gunnar-db")
	.WithDataVolume()
	.WithLifetime(ContainerLifetime.Persistent);

var ai = builder.AddOllama("gunnar-ai")
		.WithGPUSupport(OllamaGpuVendor.Nvidia)
		.WithDataVolume()
		.WithLifetime(ContainerLifetime.Persistent)
    .AddModel("nomic-embed-text");

var chatbot = builder.AddProject<Projects.Chatbot>("chatbot")
    .WithReference(db)
		.WaitFor(db)
    .WithReference(ai)
		.WaitFor(ai);

builder.Build().Run();