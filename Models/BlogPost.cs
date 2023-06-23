using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using System.Text.Json.Serialization;

namespace AzCogCli.Models;

public partial class BlogPost {
  [SimpleField(IsKey = true, IsFilterable = true)]
  public string Id { get; set; }

  [SearchableField(IsFilterable = false, IsSortable = true)]
  public string Title { get; set; }

  [SearchableField(IsFilterable = false, IsSortable = false, IsFacetable = false)]
  public string? Description { get; set; }

  [SimpleField(IsKey = false, IsFilterable = true)]
  public string Url { get; set; }

  [SimpleField(IsFilterable = true, IsSortable = true)]
  [JsonPropertyName("date_published")]
  public DateTimeOffset? DatePublished { get; set; }

  [SearchableField(IsFilterable = true, IsFacetable = true)]
  public string[]? Tags { get; set; }

  [SearchableField(IsFilterable = true, IsFacetable = true)]
  public string? Category { get; set; }

  [SearchableField]
  [JsonPropertyName("content_text")]
  public string? Content { get; set; }

}