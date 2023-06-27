namespace AzCogCli;

using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;

public class SearchClientCreator {
    public const string SearchServiceEndpointKey = "AZURE__SEARCH__ENDPOINT";
    public const string SearchServiceAdminApiKey = "AZURE__SEARCH__ADMINKEY";
    public const string SearchServiceQueryApiKey = "AZURE__SEARCH__QUERYKEY";
    public static SearchIndexClient CreateSearchIndexClient(string? endpoint = null, string? apiKey = null) {
        var _endpoint = endpoint ?? Environment.GetEnvironmentVariable(SearchServiceEndpointKey) ?? throw new ArgumentNullException(nameof(endpoint));
        var _apikey = apiKey ?? Environment.GetEnvironmentVariable(SearchServiceAdminApiKey) ?? throw new ArgumentNullException(nameof(apiKey));

        return new SearchIndexClient(new Uri(_endpoint), new AzureKeyCredential(_apikey));
    }

    public static SearchClient CreateSearchClient(string indexName, string? endpoint = null, string? apiKey = null) {
        var _endpoint = endpoint ?? Environment.GetEnvironmentVariable(SearchServiceEndpointKey) ?? throw new ArgumentNullException(nameof(endpoint));
        var _apikey = apiKey ?? Environment.GetEnvironmentVariable(SearchServiceQueryApiKey) ?? throw new ArgumentNullException(nameof(apiKey));

        return new SearchClient(new Uri(_endpoint), indexName, new AzureKeyCredential(_apikey));
    }
}