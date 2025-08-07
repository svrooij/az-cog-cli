using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using System.Reflection;
using System.Text.Json;
using AzCogCli.Models;

namespace AzCogCli.Extensions;

public static class BlogPostExtensions
{
    /// <summary>
    /// Creates search fields for BlogPost with optional vector search configuration
    /// </summary>
    public static SearchField[] CreateSearchFields(bool enableVectorSearch = false)
    {
        var fieldBuilder = new FieldBuilder();
        var fields = fieldBuilder.Build(typeof(BlogPost)).ToList();
        
        if (enableVectorSearch)
        {
            // Add vector fields manually when vector search is enabled
            var titleVectorField = new SearchField("TitleVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
            {
                IsSearchable = false,
                IsFilterable = false,
                IsSortable = false,
                IsFacetable = false,
                VectorSearchDimensions = 1536,
                VectorSearchProfileName = "my-vector-profile"
            };
            
            var contentVectorField = new SearchField("ContentVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
            {
                IsSearchable = false,
                IsFilterable = false,
                IsSortable = false,
                IsFacetable = false,
                VectorSearchDimensions = 1536,
                VectorSearchProfileName = "my-vector-profile"
            };
            
            fields.Add(titleVectorField);
            fields.Add(contentVectorField);
        }
        else
        {
            // Remove any vector fields if they exist (in case FieldBuilder picked them up)
            fields.RemoveAll(f => f.Name == "TitleVector" || f.Name == "ContentVector");
        }
        
        return fields.ToArray();
    }
    
    /// <summary>
    /// Prepares a BlogPost for indexing by removing vector data if not supported
    /// </summary>
    public static BlogPost PrepareForIndexing(this BlogPost post, bool vectorSearchEnabled)
    {
        if (!vectorSearchEnabled)
        {
            // Clear vector data to prevent upload issues
            post.TitleVector = null;
            post.ContentVector = null;
        }
        
        return post;
    }
    
    /// <summary>
    /// Converts BlogPost to a dynamic object suitable for Azure Search upload
    /// This ensures only supported fields are included in the upload
    /// </summary>
    public static object ToSearchDocument(this BlogPost post, bool vectorSearchEnabled)
    {
        var doc = new Dictionary<string, object?>
        {
            ["Id"] = post.Id,
            ["Title"] = post.Title,
            ["Description"] = post.Description,
            ["Url"] = post.Url,
            ["date_published"] = post.DatePublished,
            ["Tags"] = post.Tags,
            ["Category"] = post.Category,
            ["content_text"] = post.Content
        };
        
        // Only include vector fields if vector search is enabled
        if (vectorSearchEnabled)
        {
            if (post.TitleVector.HasValue)
            {
                doc["TitleVector"] = post.TitleVector.Value.ToArray();
            }
            
            if (post.ContentVector.HasValue)
            {
                doc["ContentVector"] = post.ContentVector.Value.ToArray();
            }
        }
        
        return doc;
    }
    
    /// <summary>
    /// Converts a collection of BlogPost objects to search documents with batching
    /// </summary>
    public static IEnumerable<IEnumerable<object>> ToSearchDocumentBatches(this IEnumerable<BlogPost> posts, bool vectorSearchEnabled, int batchSize = 100)
    {
        return posts
            .Select(post => post.ToSearchDocument(vectorSearchEnabled))
            .Chunk(batchSize);
    }
    
    /// <summary>
    /// Converts BlogPost to a dynamic object based on actual index schema
    /// This ensures only fields that exist in the target index are included
    /// </summary>
    public static object ToSearchDocument(this BlogPost post, SearchIndex indexSchema)
    {
        var doc = new Dictionary<string, object?>();
        
        // Create a mapping of BlogPost properties to their potential field names
        var propertyMappings = new Dictionary<string, Func<BlogPost, object?>>
        {
            ["Id"] = p => p.Id,
            ["Title"] = p => p.Title,
            ["Description"] = p => p.Description,
            ["Url"] = p => p.Url,
            ["date_published"] = p => p.DatePublished,
            ["Tags"] = p => p.Tags,
            ["Category"] = p => p.Category,
            ["content_text"] = p => p.Content,
            ["TitleVector"] = p => p.TitleVector?.ToArray(),
            ["ContentVector"] = p => p.ContentVector?.ToArray()
        };
        
        // Only include fields that exist in the target index schema
        foreach (var field in indexSchema.Fields)
        {
            if (propertyMappings.TryGetValue(field.Name, out var getValue))
            {
                var value = getValue(post);
                if (value != null)
                {
                    doc[field.Name] = value;
                }
            }
        }
        
        return doc;
    }
    
    /// <summary>
    /// Gets information about field mapping for logging purposes
    /// </summary>
    public static string GetFieldMappingInfo(SearchIndex indexSchema)
    {
        var availableFields = new[]
        {
            "Id", "Title", "Description", "Url", "date_published", 
            "Tags", "Category", "content_text", "TitleVector", "ContentVector"
        };
        
        var mappedFields = indexSchema.Fields
            .Where(f => availableFields.Contains(f.Name))
            .Select(f => f.Name)
            .ToList();
            
        var vectorFields = mappedFields.Where(f => f.Contains("Vector")).ToList();
        var regularFields = mappedFields.Where(f => !f.Contains("Vector")).ToList();
        
        var info = $"Mapped {mappedFields.Count} fields: {string.Join(", ", regularFields)}";
        if (vectorFields.Any())
        {
            info += $" + {vectorFields.Count} vector field(s): {string.Join(", ", vectorFields)}";
        }
        
        return info;
    }
    
    /// <summary>
    /// Converts a collection of BlogPost objects to search documents with batching based on index schema
    /// </summary>
    public static IEnumerable<IEnumerable<object>> ToSearchDocumentBatches(this IEnumerable<BlogPost> posts, SearchIndex indexSchema, int batchSize = 1000)
    {
        return posts
            .Select(post => post.ToSearchDocument(indexSchema))
            .Chunk(batchSize);
    }
}
