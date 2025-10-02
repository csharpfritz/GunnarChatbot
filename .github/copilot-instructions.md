# Gunnar Glasses Discord Chatbot

## 🧠 Pro## 📝 Dev Notes
- **Architecture**: Use .NET Aspire for orchestrating all services and dependencies
- **Modular Design**: Separate message handling, embedding logic, and Qdrant queries
- **Ollama Integration**: Use local Ollama models for generating embeddings and AI responses
- **Service Discovery**: Leverage Aspire's service discovery for connecting to Qdrant and Ollama
- **Rich Messages**: Use `EmbedBuilder` for Discord rich embeds
- **Analytics**: Log user queries for future training or analytics
- **Brand Voice**: Keep responses concise, friendly, and brand-aligned with Gunnar's tone
- **Local Development**: Ensure Ollama is running locally for embeddings and inferenceOverview
This is a Discord chatbot built in C# for the Gunnar Glasses community. It runs in a Docker container and provides helpful responses to users about Gunnar eyewear. The bot answers questions, recommends products, and shares images and links from the Gunnar website.

## ⚙️ Tech Stack
- **Language**: C#
- **Framework**: .NET 10
- **Discord API**: Discord.Net
- **Orchestration**: .NET Aspire (for resource coordination and service management)
- **Containerization**: Docker
- **Semantic Search**: Qdrant (Vector Database)
- **Embeddings**: Ollama (local LLM for vectorization and embeddings)
- **Local AI**: Ollama for local language model inference

## 💬 Core Features
- Respond to questions like:
  - “What glasses are best for gaming?”
  - “Which models support prescription lenses?”
  - “What fits best for a larger head?”
  - “I love Blizzard games — what should I wear?”
- Recommend products using semantic search over Gunnar product data.
- Share product images and links using Discord rich embeds.
- Use vector embeddings to match user queries with product descriptions.

## 🧪 Optional Features
- Personalized quiz for product recommendations.
- Order status and customer support responses.
- Care and cleaning tips.
- Promotions and discount alerts.
- Social showcase (e.g., Instagram gallery).
- Easter eggs and gamer-themed responses.

## 📦 Deployment
- **Aspire Orchestration**: Use .NET Aspire for coordinating services and dependencies
- **Service Dependencies**:
  - Discord Bot (main application)
  - Qdrant vector database
  - Ollama service for embeddings
- **Environment Variables**:
  - `DISCORD_TOKEN`: Discord bot token
  - `QDRANT_URL`: Vector DB endpoint
  - `OLLAMA_URL`: Local Ollama service endpoint
- **Aspire Configuration**: Define all services in the Aspire host project

## 📝 Dev Notes
- Use modular design: separate message handling, embedding logic, and Qdrant queries.
- Use `EmbedBuilder` for rich Discord messages.
- Log user queries for future training or analytics.
- Keep responses concise, friendly, and brand-aligned with Gunnar’s tone.
