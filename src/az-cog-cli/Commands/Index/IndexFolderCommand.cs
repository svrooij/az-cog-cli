using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using Azure.Search.Documents.Models;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using AzCogCli.Extensions;

namespace AzCogCli.Commands.Index;

public class IndexFolderCommand: Command {
  private readonly Argument<string> folderArgument;
  private readonly Option<string> queryOption;
  private readonly Option<string> indexNameOption;
  private readonly Option<string> endpointOption;
  private readonly Option<string> apiKeyOption;
  private readonly Option<bool> forceVectorSearchOption;
  
  public IndexFolderCommand(): base("folder", "Index a folder of files 📂")
  {
    // Define the arguments and options for the command
    // This allows users to specify the folder, query, index name, endpoint, and API key
    // The command will look for files matching the query in the specified folder
    // and upload them to the specified index in Azure Search
    folderArgument = new Argument<string>("folder", "Folder to look for files");
    AddArgument(folderArgument);
    queryOption = new Option<string>("--query", () => "index.json", "What files to look for");
    AddOption(queryOption);
    indexNameOption = new Option<string>("--index", () => "blog-1", "Which index shall be used?");
    AddOption(indexNameOption);
    endpointOption = new Option<string>("--endpoint", "Specify the endpoint");
    AddOption(endpointOption);
    apiKeyOption = new Option<string>("--apiKey", "Specify the Admin API Key");
    AddOption(apiKeyOption);
    forceVectorSearchOption = new Option<bool>("--force-vector-search", "Force include vector fields (overrides auto-detection)");
    AddOption(forceVectorSearchOption);
    this.SetHandler(handleCommandWithContext);
  }

  private async Task handleCommandWithContext(InvocationContext context)
  {
    var folder = context.ParseResult.GetValueForArgument(folderArgument);
    var query = context.ParseResult.GetValueForOption(queryOption)!; // Has default value
    var indexName = context.ParseResult.GetValueForOption(indexNameOption)!; // Has default value
    var endpoint = context.ParseResult.GetValueForOption(endpointOption);
    var apiKey = context.ParseResult.GetValueForOption(apiKeyOption);
    var forceVectorSearch = context.ParseResult.GetValueForOption(forceVectorSearchOption);
    var cancellationToken = context.GetCancellationToken();
    
    await handleCommand(folder, query, indexName, endpoint, apiKey, forceVectorSearch, cancellationToken);
  }

  private async Task handleCommand(string folder, string query, string indexName, string? endpoint, string? apiKey, bool forceVectorSearch, CancellationToken cancellationToken = default) {
    try
    {
      Console.WriteLine($"🔍 Searching for files matching '{query}' in folder: {folder}");
      
      // Get the actual index schema to determine available fields
      var indexClient = SearchClientCreator.CreateSearchIndexClient(endpoint, apiKey);
      SearchIndex? indexSchema = null;
      
      try 
      {
        var indexResponse = await indexClient.GetIndexAsync(indexName, cancellationToken);
        indexSchema = indexResponse.Value;
        Console.WriteLine($"📋 Retrieved index schema for '{indexName}' with {indexSchema.Fields.Count} fields");
        
        // Show detailed field mapping information
        var mappingInfo = BlogPostExtensions.GetFieldMappingInfo(indexSchema);
        Console.WriteLine($"� {mappingInfo}");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"⚠️  Could not retrieve index schema for '{indexName}': {ex.Message}");
        Console.WriteLine("📄 Falling back to basic field detection");
        
        // Fallback to the old method if we can't get the schema
        var vectorSearchEnabled = forceVectorSearch || await CheckVectorSearchSupport(indexClient, indexName, cancellationToken);
        if (forceVectorSearch)
        {
          Console.WriteLine("🔧 Vector search forced via command line option");
        }
        else if (vectorSearchEnabled)
        {
          Console.WriteLine("📊 Target index supports vector search - vector fields will be included");
        }
        else
        {
          Console.WriteLine("📄 Target index uses traditional search - vector fields will be excluded");
        }
      }
      
      var files = Directory.GetFiles(folder, query, SearchOption.AllDirectories);

      if (files.Length > 0) {
        Console.WriteLine($"📁 Found {files.Length} file(s) to process");
        
        var collection = new List<Models.BlogPost>();
        var options = new JsonSerializerOptions {
          PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        foreach(var file in files) {
          // Check for cancellation before processing each file
          cancellationToken.ThrowIfCancellationRequested();
          
          Console.WriteLine($"📄 Processing file: {Path.GetFileName(file)}");
          
          using var stream = File.OpenRead(file);
          var post = await JsonSerializer.DeserializeAsync<Models.BlogPost>(stream, options, cancellationToken);
          if (post is null) {
            Console.WriteLine($"⚠️  Skipping invalid file: {Path.GetFileName(file)}");
            continue;
          }
          post.FixCollections();
          collection.Add(post);
        }

        if (collection.Any()) {
          // Process documents using schema-based approach if available, otherwise fallback
          IEnumerable<IEnumerable<object>> batches;
          
          if (indexSchema != null)
          {
            Console.WriteLine($"📦 Using schema-based field mapping for {collection.Count} documents");
            batches = collection.ToSearchDocumentBatches(indexSchema, batchSize: 1000);
          }
          else
          {
            // Fallback to the boolean-based approach
            var vectorSearchEnabled = forceVectorSearch || await CheckVectorSearchSupport(indexClient, indexName, cancellationToken);
            Console.WriteLine($"📦 Using fallback field mapping for {collection.Count} documents");
            batches = collection.ToSearchDocumentBatches(vectorSearchEnabled, batchSize: 1000);
          }
          
          var totalBatches = batches.Count();
          var currentBatch = 0;
          
          Console.WriteLine($"📦 Processing {collection.Count} documents in {totalBatches} batch(es)");
          
          foreach (var batch in batches)
          {
            currentBatch++;
            Console.WriteLine($"📤 Uploading batch {currentBatch}/{totalBatches}...");
            
            await UploadDocuments(indexName, batch, endpoint, apiKey, cancellationToken);
            
            // Small delay between batches to be gentle on the service
            if (currentBatch < totalBatches)
            {
              await Task.Delay(100, cancellationToken);
            }
          }
          
          Console.WriteLine($"🎉 All {collection.Count} documents processed successfully!");
        }
        else {
          Console.WriteLine("⚠️  No valid documents found to index");
        }
      }
      else {
        Console.WriteLine($"❌ No files matching '{query}' found in {folder}");
      }
    }
    catch (OperationCanceledException)
    {
      Console.WriteLine("⏹️  Indexing operation was cancelled by user.");
      throw; // Re-throw to maintain proper cancellation behavior
    }
    catch (Exception ex)
    {
      Console.WriteLine($"❌ Error during indexing: {ex.Message}");
      throw; // Re-throw to maintain proper error handling
    }
  }

  private async Task<bool> CheckVectorSearchSupport(SearchIndexClient indexClient, string indexName, CancellationToken cancellationToken)
  {
    try
    {
      var index = await indexClient.GetIndexAsync(indexName, cancellationToken);
      return index.Value.VectorSearch != null && index.Value.VectorSearch.Profiles.Any();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"⚠️  Could not check vector search support for index '{indexName}': {ex.Message}");
      Console.WriteLine("📄 Defaulting to traditional search mode");
      return false;
    }
  }

  private async Task UploadDocuments<T>(string indexName, IEnumerable<T> documents, string? endpoint, string? apiKey, CancellationToken cancellationToken = default) {
    try
    {
      var documentCount = documents.Count();
      Console.WriteLine($"📝 Starting to index {documentCount} items to index '{indexName}'...");
      
      // Check for cancellation before starting the upload
      cancellationToken.ThrowIfCancellationRequested();
      
      var searchClient = SearchClientCreator.CreateSearchIndexClient(endpoint, apiKey).GetSearchClient(indexName);
      var batch = IndexDocumentsBatch.MergeOrUpload(documents);
      
      var result = await searchClient.IndexDocumentsAsync(batch, new Azure.Search.Documents.IndexDocumentsOptions { ThrowOnAnyError = false }, cancellationToken);
      
      // Check for any failed documents
      if (result.Value.Results.Any(r => !r.Succeeded))
      {
        var failedCount = result.Value.Results.Count(r => !r.Succeeded);
        var successCount = result.Value.Results.Count(r => r.Succeeded);
        
        Console.WriteLine($"⚠️  {successCount} documents indexed successfully, {failedCount} failed");
        
        // Show details of first few failures
        var failures = result.Value.Results.Where(r => !r.Succeeded).Take(3);
        foreach (var failure in failures)
        {
          Console.WriteLine($"   ❌ Document '{failure.Key}': {failure.ErrorMessage}");
        }
        
        if (failedCount > 3)
        {
          Console.WriteLine($"   ... and {failedCount - 3} more failures");
        }
      }
      else
      {
        Console.WriteLine($"✅ Successfully indexed all {documentCount} items to '{indexName}'!");
      }
    }
    catch (OperationCanceledException)
    {
      Console.WriteLine("⏹️  Document upload was cancelled by user.");
      throw; // Re-throw to maintain proper cancellation behavior
    }
    catch (Exception ex)
    {
      Console.WriteLine($"❌ Error uploading documents: {ex.Message}");
      
      // Provide more specific guidance based on common error scenarios
      if (ex.Message.Contains("field") && ex.Message.Contains("not found"))
      {
        Console.WriteLine("💡 This might be due to field mismatch between your data and the index schema.");
        Console.WriteLine("   Try recreating the index or check field names and types.");
      }
      else if (ex.Message.Contains("vector"))
      {
        Console.WriteLine("💡 This might be a vector search configuration issue.");
        Console.WriteLine("   Try using --force-vector-search or recreate the index with vector support.");
      }
      
      throw; // Re-throw to maintain proper error handling
    }
  } 
}