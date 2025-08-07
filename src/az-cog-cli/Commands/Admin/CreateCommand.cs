using System.CommandLine;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace AzCogCli.Commands.Admin;

public class CreateCommand: Command {
  public CreateCommand(): base("create", "Create the index in your search service")
  {
    var indexNameArgument = new Argument<string>("indexName", "Name of the index to create");
    var enableVectorSearchOption = new Option<bool>("--enable-vector-search", "Enable vector search capabilities (increases storage costs)");
    
    AddArgument(indexNameArgument);
    AddOption(enableVectorSearchOption);
    this.SetHandler(handleCommand, indexNameArgument, enableVectorSearchOption);
  }

  private async Task handleCommand(string indexName, bool enableVectorSearch) {
    var client = SearchClientCreator.CreateSearchIndexClient();
    var fieldBuilder = new FieldBuilder();
    var searchFields = fieldBuilder.Build(typeof(Models.BlogPost));
    
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
              new SemanticField("Content")
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

    await client.CreateOrUpdateIndexAsync(definition, true);
  }
}