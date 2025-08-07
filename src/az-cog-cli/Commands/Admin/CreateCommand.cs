using System.CommandLine;
using System.CommandLine.Invocation;
using Azure.Search.Documents.Indexes.Models;
using AzCogCli.Extensions;

namespace AzCogCli.Commands.Admin;

public sealed class CreateCommand: Command {
  private readonly Argument<string> indexNameArgument;
  private readonly Option<bool> enableVectorSearchOption;
  
  public CreateCommand(): base("create", "Create the index in your search service 🔍")
  {
    // Define the arguments and options for the command
    // This allows users to specify the index name and whether to enable vector search
    indexNameArgument = new Argument<string>("indexName", "Name of the index to create");
    enableVectorSearchOption = new Option<bool>("--enable-vector-search", "Enable vector search capabilities (increases storage costs)");
    
    AddArgument(indexNameArgument);
    AddOption(enableVectorSearchOption);
    this.SetHandler(handleCommandWithContext);
  }

  private async Task handleCommandWithContext(InvocationContext context)
  {
    var indexName = context.ParseResult.GetValueForArgument(indexNameArgument);
    var enableVectorSearch = context.ParseResult.GetValueForOption(enableVectorSearchOption);
    var cancellationToken = context.GetCancellationToken();
    
    await handleCommand(indexName, enableVectorSearch, context, cancellationToken);
  }

  private async Task handleCommand(string indexName, bool enableVectorSearch, InvocationContext context, CancellationToken cancellationToken = default) {
    try 
    {
      Console.WriteLine($"Creating index '{indexName}'...");
      if (enableVectorSearch)
      {
        Console.WriteLine("Vector search enabled - this may take longer and increase costs.");
      }
      
      var client = SearchClientCreator.CreateSearchIndexClient();
      
      // Use our custom field builder that handles vector fields conditionally
      var searchFields = BlogPostExtensions.CreateSearchFields(enableVectorSearch);
      
      var definition = new SearchIndex(indexName, searchFields);
      
      // Only add vector search configuration if explicitly enabled
      if (enableVectorSearch)
      {
        definition.VectorSearch = new VectorSearch()
        {
          Profiles =
          {
            new VectorSearchProfile("my-vector-profile", "my-hnsw-config")
          },
          Algorithms =
          {
            new HnswAlgorithmConfiguration("my-hnsw-config")
          }
        };
        
        definition.SemanticSearch = new SemanticSearch()
        {
          Configurations =
          {
            new SemanticConfiguration("my-semantic-config", new()
            {
              TitleField = new SemanticField("Title"),
              ContentFields =
              {
                new SemanticField("content_text")
              },
              KeywordsFields =
              {
                new SemanticField("Tags"),
                new SemanticField("Category")
              }
            })
          }
        };
      }

      await client.CreateOrUpdateIndexAsync(definition, allowIndexDowntime: true, cancellationToken: cancellationToken);
      
      Console.WriteLine($"✅ Index '{indexName}' created successfully!");
      if (enableVectorSearch)
      {
        Console.WriteLine("📇 Vector search and semantic search are now enabled.");
      }
    }
    catch (OperationCanceledException)
    {
      Console.WriteLine("🛑 Index creation was cancelled by user.");
      context.ExitCode = 10; // Set exit code to indicate cancellation
      throw; // Re-throw to maintain proper cancellation behavior
    }
    catch (Exception ex)
    {
      Console.WriteLine($"❌ Error creating index: {ex.Message}");
      context.ExitCode = 1;
      throw; // Re-throw to maintain proper error handling
    }
  }
}