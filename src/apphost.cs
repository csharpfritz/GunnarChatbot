#:package Aspire.Hosting.Qdrant@9.5.0
#:package CommunityToolkit.Aspire.Hosting.Ollama@9.8.0
#:sdk Aspire.AppHost.Sdk@9.5.0
#:project Chatbot

var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddQdrant("gunnar-db");

var ai = builder.AddOllama("gunnar-ai")
    .AddModel("nomic-embed-text");

var chatbot = builder.AddProject<Projects.Chatbot>("chatbot")
    .WithReference(db)
    .WithReference(ai);

builder.Build().Run();