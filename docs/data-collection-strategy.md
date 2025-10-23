# Gunnar Product Data Collection & Embedding Strategy

## Overview

This document outlines the strategy for collecting, organizing, and indexing Gunnar Optiks product data into our vector embedding database. The goal is to create a comprehensive knowledge base that enables our Discord chatbot to provide accurate, contextual recommendations and information about Gunnar eyewear products.

## Data Sources

### Primary Source: Gunnar.com Website
- **Product Pages**: Individual product detail pages containing specifications, features, and descriptions
- **Category Pages**: Gaming glasses, computer glasses, prescription glasses, etc.
- **Collection Pages**: Brand collaborations (Marves, Blizzard, etc.)
- **Blog/Articles**: Product guides, fitting information, care instructions
- **Support Pages**: FAQ, sizing guides, technical specifications

### Secondary Sources (Future Implementation)
- Product reviews and ratings
- User-generated content from social media
- Customer support tickets and common questions
- Product manuals and technical documentation

## Web Crawling Strategy

### 1. Site Discovery & Mapping
```
gunnar.com/
├── /collections/
│   ├── /gaming-glasses/
│   ├── /computer-glasses/
│   ├── /prescription-glasses/
│   └── /collaborations/
├── /products/
│   └── /[product-slug]/
├── /pages/
│   ├── /fit-guide/
│   ├── /lens-guide/
│   └── /care-instructions/
└── /blogs/
    └── /guides/
```

### 2. Crawling Approach
- **Respectful Crawling**: Implement delays between requests (1-2 seconds)
- **robots.txt Compliance**: Check and respect site crawling guidelines
- **User-Agent**: Identify crawler as "Gunnar-ChatBot-DataCollector/1.0"
- **Error Handling**: Graceful handling of 404s, timeouts, and rate limits
- **Incremental Updates**: Track last-modified dates to avoid re-crawling unchanged content

### 3. Content Extraction Rules

#### Product Pages
```csharp
ProductData {
    string Name;
    string SKU;
    string Description;
    List<string> Features;
    string DefaultLensType;
    List<LensOption> SupportedLenses;
    string FrameType;
    string FrameColor;
    decimal Price;
    List<string> Images;
    string Category;
    string Collection;
    Dictionary<string, string> Specifications;
    string FitGuide;
    List<string> Tags;
}

LensOption {
    string LensType; // "Amber", "Clear", "Prescription", "Sunglass", "Dark Amber"
    string BlueightProtection; // "65%", "35%", "98%", etc.
    decimal PriceModifier; // Additional cost for this lens type
    bool IsAvailable;
    string Description;
    List<string> Benefits;
    List<string> RecommendedUses;
}
```

#### Category/Collection Pages
- Extract product listings and relationships
- Capture category-specific features and benefits
- Identify cross-selling opportunities

## Data Organization & Natural Groupings

### 1. Product Hierarchical Structure
```
Gaming Glasses
├── FPS Gaming
│   ├── Intercept (multiple variants)
│   ├── Siege (multiple variants)
│   └── MLG Phantom
├── MOBA/Strategy Gaming
│   ├── Vayper
│   └── Lightning Bolt 360
└── Console Gaming
    ├── Cruz
    └── Haus
```

### 2. Embedding Document Types

#### A. Individual Product Embeddings
Each product variant gets its own embedding document:
```json
{
  "id": "gunnar-intercept-onyx-amber",
  "type": "product",
  "content": "Gunnar Intercept Onyx with Amber lenses. Premium gaming glasses designed for FPS gaming with 65% blue light protection. Lightweight stainless steel frame in onyx finish. Features crystalline amber lenses that enhance contrast and reduce eye strain during long gaming sessions. Available in multiple lens options including Clear (35% blue light blocking), prescription lenses, and sunglass tints. The amber lenses are specifically optimized for gaming with enhanced contrast and color accuracy. Recommended for PC gaming, especially competitive FPS titles like CS:GO, Valorant, and Call of Duty.",
  "metadata": {
    "product_name": "Intercept",
    "variant": "Onyx/Amber",
    "category": "Gaming Glasses",
    "subcategory": "FPS Gaming",
    "frame_color": "Onyx",
    "default_lens_type": "Amber",
    "supported_lenses": [
      {
        "type": "Amber",
        "blue_light_protection": "65%",
        "price_modifier": 0.00,
        "benefits": ["Enhanced Contrast", "Reduced Eye Strain", "Gaming Optimized"],
        "recommended_uses": ["FPS Gaming", "Long Gaming Sessions", "Low Light Gaming"]
      },
      {
        "type": "Clear",
        "blue_light_protection": "35%",
        "price_modifier": 0.00,
        "benefits": ["Natural Color Accuracy", "All-Day Comfort", "Professional Use"],
        "recommended_uses": ["Office Work", "General Computer Use", "Video Calls"]
      },
      {
        "type": "Prescription",
        "blue_light_protection": "35%-65%",
        "price_modifier": 150.00,
        "benefits": ["Vision Correction", "Blue Light Protection", "Custom Fit"],
        "recommended_uses": ["Prescription Wearers", "All-Day Use", "Gaming with Vision Correction"]
      },
      {
        "type": "Sunglass",
        "blue_light_protection": "98%",
        "price_modifier": 25.00,
        "benefits": ["UV Protection", "Glare Reduction", "Outdoor Use"],
        "recommended_uses": ["Outdoor Gaming", "Driving", "Bright Environments"]
      }
    ],
    "blue_light_protection": "35%-65%",
    "prescription_available": true,
    "sunglass_available": true,
    "price": 99.99,
    "target_use": ["FPS Gaming", "PC Gaming", "Long Gaming Sessions"]
  }
}
```

#### B. Category-Level Embeddings
Broader category information for general questions:
```json
{
  "id": "gunnar-gaming-glasses-overview",
  "type": "category",
  "content": "Gunnar gaming glasses are specifically designed to reduce digital eye strain and improve visual performance during gaming. All gaming glasses feature blue light filtering technology, anti-reflective coatings, and ergonomic frame designs. Available in various lens tints including Amber (65% blue light blocking), Clear (35% blue light blocking), and specialized gaming tints.",
  "metadata": {
    "category": "Gaming Glasses",
    "key_benefits": ["Eye Strain Reduction", "Blue Light Protection", "Enhanced Contrast"],
    "target_audience": ["Gamers", "Esports Players", "Streamers"]
  }
}
```

#### C. Use-Case Embeddings
Scenario-based information for recommendations:
```json
{
  "id": "gunnar-prescription-gaming-setup",  
  "type": "use_case",
  "content": "For gamers who wear prescription glasses, Gunnar offers prescription gaming glasses through their Rx program. Choose from popular gaming frames like Intercept, Siege, or Vayper and add your prescription. Prescription gaming glasses provide the same blue light protection and gaming optimization as regular Gunnar glasses while correcting vision. Available with single vision, progressive, or specialized gaming prescriptions.",
  "metadata": {
    "use_case": "Prescription Gaming",
    "target_users": ["Prescription Wearers", "Gamers with Vision Correction"],
    "available_frames": ["Intercept", "Siege", "Vayper", "Haus"]
  }
}
```

#### D. Feature/Technology Embeddings
Technical information about Gunnar technologies:
```json
{
  "id": "gunnar-blue-light-technology",
  "type": "technology",
  "content": "Gunnar's proprietary blue light filtering technology blocks harmful high-energy visible (HEV) blue light from digital screens. Available in multiple protection levels: Clear lenses block 35% for natural color accuracy, Amber lenses block 65% with enhanced contrast for gaming, Dark Amber blocks 80% for maximum protection, and Sunglass tints block up to 98% with UV protection. Each lens type is optimized for specific use cases while reducing eye strain, improving sleep quality, and enhancing visual performance.",
  "metadata": {
    "technology": "Blue Light Filtering",
    "lens_types": {
      "Clear": {
        "protection": "35%",
        "best_for": "Office work, professional use",
        "color_accuracy": "Natural"
      },
      "Amber": {
        "protection": "65%",
        "best_for": "Gaming, evening use",
        "color_accuracy": "Enhanced contrast"
      },
      "Dark Amber": {
        "protection": "80%",
        "best_for": "Light sensitivity, medical use",
        "color_accuracy": "Significant warm shift"
      },
      "Sunglass": {
        "protection": "85-98%",
        "best_for": "Outdoor use, bright environments",
        "color_accuracy": "Various tint options"
      }
    },
    "benefits": ["Reduced Eye Strain", "Better Sleep", "Enhanced Contrast", "UV Protection"]
  }
}
```

#### E. Lens-Specific Embeddings
Detailed information about specific lens types and their benefits:
```json
{
  "id": "gunnar-prescription-lens-options",
  "type": "lens_technology",
  "content": "Gunnar prescription lenses combine vision correction with blue light protection technology. Available in single vision, progressive, and bifocal options. Prescription lenses can be made with Clear base (35% blue light blocking) for natural color accuracy, or Amber base (65% blue light blocking) for enhanced gaming performance. Progressive prescription lenses are ideal for users who need both distance and reading correction while using computers. Custom prescription manufacturing ensures optimal fit and vision correction while maintaining Gunnar's eye protection benefits.",
  "metadata": {
    "lens_category": "Prescription",
    "prescription_types": ["Single Vision", "Progressive", "Bifocal", "Reading Only"],
    "base_lens_options": ["Clear (35% protection)", "Amber (65% protection)"],
    "benefits": ["Vision Correction", "Blue Light Protection", "Custom Fit", "Professional Quality"],
    "target_users": ["Prescription Wearers", "Progressive Lens Users", "Reading Glass Users"],
    "price_range": "$249-$399",
    "manufacturing_time": "7-14 business days"
  }
}
```

### 3. Lens Type Classification & Support

#### Gunnar Lens Types Overview
```json
{
  "lens_types": {
    "amber": {
      "name": "Amber/Crystalline",
      "blue_light_protection": "65%",
      "color_enhancement": "High contrast, warm tint",
      "best_for": ["Gaming", "Low light environments", "Evening computer use"],
      "characteristics": ["Enhanced contrast", "Reduced eye strain", "Improved sleep patterns"]
    },
    "clear": {
      "name": "Clear/Liquet",
      "blue_light_protection": "35%",
      "color_enhancement": "Natural color accuracy",
      "best_for": ["Office work", "Professional environments", "All-day computer use"],
      "characteristics": ["Minimal color distortion", "Subtle protection", "Professional appearance"]
    },
    "dark_amber": {
      "name": "Dark Amber/Umber",
      "blue_light_protection": "80%",
      "color_enhancement": "Maximum contrast enhancement",
      "best_for": ["Severe light sensitivity", "Post-surgery recovery", "Maximum protection"],
      "characteristics": ["Highest protection level", "Significant color shift", "Medical grade filtering"]
    },
    "prescription": {
      "name": "Prescription (Rx)",
      "blue_light_protection": "Varies by base lens",
      "color_enhancement": "Based on selected lens type",
      "best_for": ["Vision correction needed", "Custom prescriptions", "Progressive lenses"],
      "characteristics": ["Custom vision correction", "Available in multiple lens types", "Single vision or progressive"],
      "additional_info": {
        "prescription_types": ["Single Vision", "Progressive", "Bifocal", "Reading"],
        "available_lens_bases": ["Clear", "Amber", "Sunglass tints"],
        "typical_cost_addition": "$150-$300"
      }
    },
    "sunglass": {
      "name": "Sunglass Tints",
      "blue_light_protection": "85-98%",
      "color_enhancement": "Various tint options",
      "best_for": ["Outdoor use", "Bright environments", "UV protection"],
      "characteristics": ["UV protection", "Glare reduction", "Multiple tint options"],
      "tint_options": ["Grey", "Brown", "Green", "Gradient"]
    },
    "photochromic": {
      "name": "Photochromic/Transitions",
      "blue_light_protection": "Variable (35-85%)",
      "color_enhancement": "Adaptive based on lighting",
      "best_for": ["Variable lighting", "Indoor/outdoor transitions", "All-day wear"],
      "characteristics": ["Automatically adjusts", "Light-responsive", "Convenience factor"]
    }
  }
}
```

### 4. Content Optimization for Embeddings

#### Text Processing Pipeline
1. **HTML Cleaning**: Remove markup, preserve semantic structure
2. **Content Normalization**: Standardize product names, specifications
3. **Feature Extraction**: Identify key product attributes automatically
4. **Content Enhancement**: Add contextual information and cross-references
5. **Chunking Strategy**: Break long content into semantically meaningful chunks

#### Natural Language Enhancement
- **Expand Abbreviations**: "FPS" → "First Person Shooter", "Rx" → "Prescription"
- **Add Context**: "Amber lenses" → "Amber tinted lenses that filter 65% of blue light and enhance contrast"
- **Include Synonyms**: "Gaming glasses" → "Gaming eyewear, computer glasses for gaming"
- **Lens-Specific Terms**: 
  - "Clear" → "Clear lenses with natural color accuracy and 35% blue light protection"
  - "Prescription" → "Custom prescription lenses with blue light filtering technology"
  - "Sunglass" → "Tinted sunglass lenses with UV protection and maximum blue light blocking"
  - "Dark Amber" → "Maximum protection amber lenses with 80% blue light filtering"
- **Add Use Cases**: Connect products to specific gaming scenarios and lens recommendations
- **Protection Levels**: Always specify blue light protection percentages with lens types

## Implementation Architecture

### 1. Web Crawler Service
```csharp
public interface IWebCrawlerService
{
    Task<List<ProductData>> CrawlProductsAsync();
    Task<List<CategoryData>> CrawlCategoriesAsync();
    Task<List<ContentData>> CrawlSupportContentAsync();
    Task<CrawlResult> IncrementalCrawlAsync(DateTime lastCrawl);
}
```

### 2. Content Processor Service
```csharp
public interface IContentProcessorService
{
    Task<List<EmbeddingDocument>> ProcessProductDataAsync(List<ProductData> products);
    Task<EmbeddingDocument> CreateProductEmbeddingAsync(ProductData product);
    Task<EmbeddingDocument> CreateCategoryEmbeddingAsync(CategoryData category);
    Task<List<EmbeddingDocument>> CreateCrossReferenceEmbeddingsAsync(List<ProductData> products);
}
```

### 3. Embedding Pipeline
```csharp
public interface IEmbeddingPipelineService
{
    Task IndexProductCatalogAsync();
    Task UpdateProductAsync(string productId);
    Task<List<EmbeddingDocument>> SearchSimilarProductsAsync(string query);
    Task ValidateEmbeddingQualityAsync();
}
```

## Data Quality & Validation

### 1. Content Validation Rules
- **Completeness**: All products must have name, description, category
- **Consistency**: Standardized naming conventions and categorization
- **Accuracy**: Cross-reference with official product specifications
- **Freshness**: Regular updates to pricing and availability

### 2. Embedding Quality Metrics
- **Semantic Coherence**: Similar products should have similar embeddings
- **Query Performance**: Test with common user questions
- **Recommendation Accuracy**: Validate product recommendations
- **Coverage**: Ensure all product categories are well-represented

### 3. Testing Strategy
- **Unit Tests**: Individual component validation
- **Integration Tests**: End-to-end crawling and indexing
- **User Acceptance Tests**: Test with real user queries
- **Performance Tests**: Embedding generation and search speed

## Deployment & Maintenance

### 1. Initial Data Collection
1. Full site crawl and initial indexing
2. Manual review and quality assurance
3. Test embeddings with sample queries
4. Deploy to production with monitoring

### 2. Ongoing Maintenance
- **Daily**: Monitor for new products and updates
- **Weekly**: Incremental crawl and index updates
- **Monthly**: Full data quality review and optimization
- **Quarterly**: Review and update categorization strategy

### 3. Monitoring & Alerts
- Crawl success/failure rates
- Embedding generation performance
- Search query performance
- User interaction patterns and feedback

## Future Enhancements

### 1. Advanced Features
- **Multi-modal Embeddings**: Include product images in embeddings
- **Seasonal Adjustments**: Weight products based on gaming seasons/releases
- **User Behavior Learning**: Improve recommendations based on Discord interactions
- **Real-time Updates**: Live product availability and pricing updates

### 2. Integration Opportunities
- **Review Systems**: Incorporate customer reviews and ratings
- **Social Media**: Monitor mentions and user-generated content
- **Partner Content**: Include content from gaming partners (Razer, SteelSeries)
- **Community Feedback**: Learn from Discord bot interactions

## Success Metrics

### 1. Technical Metrics
- **Coverage**: % of Gunnar products successfully indexed
- **Freshness**: Average age of product information
- **Performance**: Embedding generation and search response times
- **Quality**: Embedding similarity scores for related products

### 2. User Experience Metrics
- **Relevance**: User satisfaction with product recommendations
- **Accuracy**: Correctness of product information provided
- **Discovery**: Users finding products they weren't initially looking for
- **Conversion**: Bot interactions leading to product interest/purchases

This strategy provides a comprehensive foundation for building a robust, accurate, and useful product knowledge base for the Gunnar Discord chatbot.