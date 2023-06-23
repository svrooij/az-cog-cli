using System.CommandLine;
using System.Text.Json;
using Azure.Search.Documents.Models;

namespace AzCogCli.Commands.Index;

public class IndexFolderCommand: Command {
  public IndexFolderCommand(): base("folder", "Add data in a folder to your index")
  {
    var folderArgument = new Argument<string>("folder", "Folder to look for files");
    AddArgument(folderArgument);
    var queryOption = new Option<string>("--query", () => "index.json", "What files to look for");
    AddOption(queryOption);
    var indexNameOption = new Option<string>("--index", () => "blog-1", "Which index shall be used?");
    AddOption(indexNameOption);
    var endpointOption = new Option<string>("--endpoint", "Specify the endpoint");
    AddOption(endpointOption);
    var apiKeyOption = new Option<string>("--apiKey", "Specify the Admin API Key");
    AddOption(apiKeyOption);
    this.SetHandler(handleCommand, folderArgument, queryOption, indexNameOption, endpointOption, apiKeyOption);
  }

  private async Task handleCommand(string folder, string query, string indexName, string? endpoint, string? apiKey) {
    var files = Directory.GetFiles(folder, query, SearchOption.AllDirectories);

    if (files.Length > 0) {
      var collection = new List<Models.BlogPost>();
      var options = new JsonSerializerOptions {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      };

      foreach(var file in files) {
        using var stream = File.OpenRead(file);
        var post = await JsonSerializer.DeserializeAsync<Models.BlogPost>(stream, options);
        if (post is null) {
          continue;
        }
        post.FixCollections();
        collection.Add(post);
      }

      if (collection.Any()) {
        await UploadDocuments(indexName, collection, endpoint, apiKey);
        
      }
    }
  }

  private async Task UploadDocuments<T>(string indexName, IEnumerable<T> documents, string? endpoint, string? apiKey, CancellationToken cancellationToken = default) {
    Console.WriteLine("📝 Start indexing {0} items", documents.Count());
    var searchClient = SearchClientCreator.CreateSearchIndexClient(endpoint, apiKey).GetSearchClient(indexName);
    var batch = IndexDocumentsBatch.MergeOrUpload<T>(documents);
    await searchClient.IndexDocumentsAsync(batch, new Azure.Search.Documents.IndexDocumentsOptions { ThrowOnAnyError = true }, cancellationToken);
    Console.WriteLine("✅ All items indexed");

  } 
}