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
}
