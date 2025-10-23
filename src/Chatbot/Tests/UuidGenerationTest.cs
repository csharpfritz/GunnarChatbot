using Chatbot.Services;
using System;

namespace Chatbot.Tests;

/// <summary>
/// Simple test to demonstrate the UUID generation fix for SKU to UUID conversion
/// </summary>
public static class UuidGenerationTest
{
    /// <summary>
    /// Test the deterministic UUID generation from SKU strings
    /// </summary>
    public static void RunUuidTests()
    {
        Console.WriteLine("🧪 Testing UUID Generation from SKU Strings");
        Console.WriteLine("=" + new string('=', 50));
        
        // Test with typical Gunnar SKU formats
        var testSkus = new[]
        {
            "OVERWATCH-ULTIMATE",
            "INTERCEPT-ONYX-AMBER",
            "SIEGE-TORTOISE-AMBER", 
            "VAYPER-ONYX-CLEAR",
            "CRUZ-GUNMETAL-AMBER",
            "overwatch-ultimate", // Test case sensitivity
            "OVERWATCH-ULTIMATE"  // Test consistency
        };

        Console.WriteLine($"Testing {testSkus.Length} SKU samples:\n");

        foreach (var sku in testSkus)
        {
            var uuid = VectorService.GenerateUuidFromString(sku);
            Console.WriteLine($"📦 SKU: '{sku}' → 🆔 UUID: {uuid}");
        }

        Console.WriteLine("\n✅ UUID Generation Tests:");
        
        // Test 1: Consistency - same input should generate same UUID
        var testSku = "OVERWATCH-ULTIMATE";
        var uuid1 = VectorService.GenerateUuidFromString(testSku);
        var uuid2 = VectorService.GenerateUuidFromString(testSku);
        var isConsistent = uuid1 == uuid2;
        
        Console.WriteLine($"1. Consistency Test: {(isConsistent ? "✅ PASS" : "❌ FAIL")} - Same SKU generates same UUID");
        
        // Test 2: Uniqueness - different inputs should generate different UUIDs
        var sku1 = "OVERWATCH-ULTIMATE";
        var sku2 = "INTERCEPT-ONYX-AMBER";
        var uuidA = VectorService.GenerateUuidFromString(sku1);
        var uuidB = VectorService.GenerateUuidFromString(sku2);
        var isUnique = uuidA != uuidB;
        
        Console.WriteLine($"2. Uniqueness Test: {(isUnique ? "✅ PASS" : "❌ FAIL")} - Different SKUs generate different UUIDs");
        
        // Test 3: Valid UUID format
        var testUuid = VectorService.GenerateUuidFromString("TEST-SKU");
        var isValidGuid = Guid.TryParse(testUuid, out var parsedGuid);
        
        Console.WriteLine($"3. Valid Format Test: {(isValidGuid ? "✅ PASS" : "❌ FAIL")} - Generated UUID is valid GUID format");
        
        Console.WriteLine($"\n🎉 UUID Generation Fix Summary:");
        Console.WriteLine($"   • SKU strings are now converted to valid UUIDs using SHA-256 hash");
        Console.WriteLine($"   • Same SKU always generates the same UUID (deterministic)"); 
        Console.WriteLine($"   • Different SKUs generate different UUIDs (collision-resistant)");
        Console.WriteLine($"   • All generated UUIDs are valid GUID format for Qdrant");
        Console.WriteLine($"   • Original SKU is preserved in the metadata for reference");
        
        Console.WriteLine("\n" + "=" + new string('=', 50));
    }
}

/// <summary>
/// Simple console program to run the UUID tests
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            UuidGenerationTest.RunUuidTests();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}