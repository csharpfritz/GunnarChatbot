using Microsoft.Extensions.AI;

namespace Chatbot.Services;

public class EmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    public EmbeddingService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        _embeddingGenerator = embeddingGenerator;
    }

    /// <summary>
    /// Generate embeddings for text using the Ollama embedding model
    /// </summary>
    /// <param name="text">The text to generate embeddings for</param>
    /// <returns>Float array representing the text embeddings</returns>
    public async Task<float[]> GenerateEmbeddingsAsync(string text)
    {
        try
        {
            var embeddings = await _embeddingGenerator.GenerateAsync([text]);
            return embeddings.FirstOrDefault()?.Vector.ToArray() ?? Array.Empty<float>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to generate embeddings for text: {text}", ex);
        }
    }

    /// <summary>
    /// Generate embeddings for multiple texts in batch
    /// </summary>
    /// <param name="texts">Collection of texts to generate embeddings for</param>
    /// <returns>Dictionary mapping text to its embeddings</returns>
    public async Task<Dictionary<string, float[]>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts)
    {
        try
        {
            var textArray = texts.ToArray();
            var embeddings = await _embeddingGenerator.GenerateAsync(textArray);
            
            var results = new Dictionary<string, float[]>();
            for (int i = 0; i < textArray.Length; i++)
            {
                if (i < embeddings.Count)
                {
                    results[textArray[i]] = embeddings[i].Vector.ToArray();
                }
            }
            
            return results;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to generate batch embeddings", ex);
        }
    }

    /// <summary>
    /// Check if the embedding generator is available
    /// </summary>
    /// <returns>True if the generator is available, false otherwise</returns>
    public async Task<bool> IsGeneratorAvailableAsync()
    {
        try
        {
            // Try to generate a simple embedding to test availability
            var testEmbeddings = await _embeddingGenerator.GenerateAsync(["test"]);
            return testEmbeddings.Any();
        }
        catch
        {
            return false;
        }
    }
}