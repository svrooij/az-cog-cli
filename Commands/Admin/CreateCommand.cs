using System.CommandLine;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace AzCogCli.Commands.Admin;

public class CreateCommand: Command {
  public CreateCommand(): base("create", "Create the index in your search service")
  {
    var indexNameArgument = new Argument<string>("indexName", "Name of the index to create");
    AddArgument(indexNameArgument);
    this.SetHandler(handleCommand, indexNameArgument);
  }

  private async Task handleCommand(string indexName) {
    var client = SearchClientCreator.CreateSearchIndexClient();
    var fieldBuilder = new FieldBuilder();
    var searchFields = fieldBuilder.Build(typeof(Models.BlogPost));
    var definition = new SearchIndex(indexName, searchFields);

    await client.CreateOrUpdateIndexAsync(definition, true);
  }
}